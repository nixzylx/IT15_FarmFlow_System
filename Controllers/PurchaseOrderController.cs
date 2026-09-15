using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Purchaser")]
public class PurchaseOrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================
    // INDEX: List all purchase orders
    // ============================================
    public async Task<IActionResult> Index(string searchTerm, string status)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => p.PONumber.Contains(searchTerm) ||
                                     p.Supplier!.CompanyName.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var orders = await query.OrderByDescending(p => p.OrderDate).ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.TotalOrders = orders.Count;
        ViewBag.PendingCount = orders.Count(p => p.Status == "Pending Approval" || p.Status == "Draft");
        ViewBag.SentCount = orders.Count(p => p.Status == "Sent" || p.Status == "Shipped");
        ViewBag.ReceivedCount = orders.Count(p => p.Status == "Received");
        ViewBag.TotalValue = orders.Sum(p => p.TotalAmount ?? 0);

        return View(orders);
    }

    // ============================================
    // DETAILS: View single PO
    // ============================================
    public async Task<IActionResult> Details(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.RequestedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.PurchaseOrderItems)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null) return NotFound();
        return View(po);
    }

    // ============================================
    // CREATE: Show form
    // ============================================
    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();

        // Generate PO number
        var count = await _context.PurchaseOrders.CountAsync();
        ViewBag.NextPONumber = $"PO-{DateTime.Now:yyyy}-{(count + 1):D4}";

        return View(new PurchaseOrder());
    }

    // ============================================
    // CREATE: Save to database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrder po,
                                             List<int> itemIds,
                                             List<decimal> quantities,
                                             List<decimal> unitPrices)
    {
        if (ModelState.IsValid)
        {
            po.CreatedDateTime = DateTime.Now;
            po.OrderDate = DateTime.Now;
            po.Status = "Draft";

            // Set current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                po.RequestedByUserId = userId;

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            // Add items
            decimal subtotal = 0;
            if (itemIds != null && itemIds.Any())
            {
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (itemIds[i] > 0 && quantities[i] > 0)
                    {
                        var lineAmount = quantities[i] * unitPrices[i];
                        subtotal += lineAmount;

                        _context.PurchaseOrderItems.Add(new PurchaseOrderItem
                        {
                            PurchaseOrderId = po.Id,
                            InventoryItemId = itemIds[i],
                            QuantityOrdered = quantities[i],
                            UnitPrice = unitPrices[i],
                            TotalLineAmount = lineAmount,
                            CreatedDateTime = DateTime.Now
                        });
                    }
                }
            }

            po.SubtotalAmount = subtotal;
            po.TotalAmount = subtotal;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Purchase Order '{po.PONumber}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = po.Id });
        }

        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();
        return View(po);
    }

    // ============================================
    // EDIT: Show form
    // ============================================
    public async Task<IActionResult> Edit(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null) return NotFound();
        if (po.Status != "Draft" && po.Status != "Pending Approval")
        {
            TempData["ErrorMessage"] = "Only Draft or Pending POs can be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();

        return View(po);
    }

    // ============================================
    // EDIT: Update database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrder po,
                                           List<int> itemIds,
                                           List<decimal> quantities,
                                           List<decimal> unitPrices)
    {
        if (id != po.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing == null) return NotFound();

            existing.SupplierId = po.SupplierId;
            existing.DeliveryExpectedDate = po.DeliveryExpectedDate;
            existing.PaymentTerms = po.PaymentTerms;
            existing.Notes = po.Notes;
            existing.ModifiedDateTime = DateTime.Now;

            // Remove old items
            _context.PurchaseOrderItems.RemoveRange(existing.PurchaseOrderItems);

            // Add new items
            decimal subtotal = 0;
            if (itemIds != null && itemIds.Any())
            {
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (itemIds[i] > 0 && quantities[i] > 0)
                    {
                        var lineAmount = quantities[i] * unitPrices[i];
                        subtotal += lineAmount;

                        _context.PurchaseOrderItems.Add(new PurchaseOrderItem
                        {
                            PurchaseOrderId = id,
                            InventoryItemId = itemIds[i],
                            QuantityOrdered = quantities[i],
                            UnitPrice = unitPrices[i],
                            TotalLineAmount = lineAmount,
                            CreatedDateTime = DateTime.Now
                        });
                    }
                }
            }

            existing.SubtotalAmount = subtotal;
            existing.TotalAmount = subtotal;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Purchase Order updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();
        return View(po);
    }

    // ============================================
    // APPROVE: Admin approves PO
    // ============================================
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            po.ApprovedByUserId = userId;

        po.Status = "Sent";
        po.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"PO '{po.PONumber}' approved and sent!";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================
    // RECEIVE: Warehouse Keeper receives goods
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null) return NotFound();

        if (po.Status != "Sent" && po.Status != "Shipped")
        {
            TempData["ErrorMessage"] = "Only Sent or Shipped POs can be received.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Update stock for each item
        foreach (var item in po.PurchaseOrderItems)
        {
            var invItem = await _context.InventoryItems.FindAsync(item.InventoryItemId);
            if (invItem != null)
            {
                var stockBefore = invItem.CurrentStock;
                invItem.CurrentStock += item.QuantityOrdered;
                invItem.ModifiedDateTime = DateTime.Now;
                item.QuantityReceived = item.QuantityOrdered;

                // Log transaction
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    InventoryItemId = invItem.Id,
                    TransactionType = "Receiving",
                    ReferenceDocument = po.PONumber,
                    QuantityChange = item.QuantityOrdered,
                    StockBefore = stockBefore,
                    StockAfter = invItem.CurrentStock,
                    UnitCost = item.UnitPrice,
                    Remarks = $"Received from PO {po.PONumber}",
                    TransactionDateTime = DateTime.Now
                });
            }
        }

        po.Status = "Received";
        po.ActualDeliveryDate = DateTime.Now;
        po.ModifiedDateTime = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"PO '{po.PONumber}' received! Stock updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================
    // CANCEL: Cancel PO
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();

        if (po.Status == "Received")
        {
            TempData["ErrorMessage"] = "Received POs cannot be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        po.Status = "Cancelled";
        po.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"PO '{po.PONumber}' cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================
    // DELETE: Show confirmation
    // ============================================
    public async Task<IActionResult> Delete(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null) return NotFound();
        return View(po);
    }

    // ============================================
    // DELETE: Remove from database
    // ============================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po != null)
        {
            _context.PurchaseOrderItems.RemoveRange(po.PurchaseOrderItems);
            _context.PurchaseOrders.Remove(po);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Purchase Order deleted!";
        }
        return RedirectToAction(nameof(Index));
    }
}