using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Sales_Rep")]
public class SalesOrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalesOrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm, string status)
    {
        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesRep)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(s => s.SONumber.Contains(searchTerm) || s.Customer!.CompanyName!.Contains(searchTerm));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var orders = await query.OrderByDescending(s => s.OrderDate).ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.TotalOrders = orders.Count;
        ViewBag.PendingCount = orders.Count(s => s.Status == "Confirmed" || s.Status == "Picking");
        ViewBag.DeliveredCount = orders.Count(s => s.Status == "Delivered");
        ViewBag.TotalValue = orders.Sum(s => s.TotalAmount ?? 0);

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesRep)
            .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive && i.CurrentStock > 0).ToListAsync();

        var count = await _context.SalesOrders.CountAsync();
        ViewBag.NextSONumber = $"SO-{DateTime.Now:yyyy}-{(count + 1):D4}";

        return View(new SalesOrder());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrder so,
                                             List<int> itemIds,
                                             List<decimal> quantities,
                                             List<decimal> unitPrices)
    {
        if (ModelState.IsValid)
        {
            so.CreatedDateTime = DateTime.Now;
            so.OrderDate = DateTime.Now;
            so.Status = "Confirmed";
            so.PaymentStatus = "Unpaid";

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                so.SalesRepId = userId;

            _context.SalesOrders.Add(so);
            await _context.SaveChangesAsync();

            decimal subtotal = 0;
            if (itemIds != null && itemIds.Any())
            {
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (itemIds[i] > 0 && quantities[i] > 0)
                    {
                        var lineAmount = quantities[i] * unitPrices[i];
                        subtotal += lineAmount;

                        _context.SalesOrderItems.Add(new SalesOrderItem
                        {
                            SalesOrderId = so.Id,
                            InventoryItemId = itemIds[i],
                            QuantityOrdered = quantities[i],
                            UnitPrice = unitPrices[i],
                            TotalLineAmount = lineAmount,
                            CreatedDateTime = DateTime.Now
                        });
                    }
                }
            }

            so.SubtotalAmount = subtotal;
            so.TotalAmount = subtotal;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Sales Order '{so.SONumber}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = so.Id });
        }

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
        ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive && i.CurrentStock > 0).ToListAsync();
        return View(so);
    }

    // Dispatch: mark as dispatched
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispatch(int id)
    {
        var so = await _context.SalesOrders.FindAsync(id);
        if (so == null) return NotFound();

        so.Status = "Dispatched";
        so.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Order '{so.SONumber}' marked as dispatched!";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Deliver: deduct stock, mark delivered
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(int id)
    {
        var so = await _context.SalesOrders
            .Include(s => s.SalesOrderItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (so == null) return NotFound();

        foreach (var item in so.SalesOrderItems)
        {
            var invItem = await _context.InventoryItems.FindAsync(item.InventoryItemId);
            if (invItem != null)
            {
                var stockBefore = invItem.CurrentStock;
                invItem.CurrentStock -= item.QuantityOrdered;
                invItem.ModifiedDateTime = DateTime.Now;
                item.QuantityDelivered = item.QuantityOrdered;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    InventoryItemId = invItem.Id,
                    TransactionType = "Issue",
                    ReferenceDocument = so.SONumber,
                    QuantityChange = -item.QuantityOrdered,
                    StockBefore = stockBefore,
                    StockAfter = invItem.CurrentStock,
                    UnitCost = item.UnitPrice,
                    Remarks = $"Sold via SO {so.SONumber}",
                    TransactionDateTime = DateTime.Now
                });
            }
        }

        so.Status = "Delivered";
        so.ActualDeliveryDate = DateTime.Now;
        so.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Order '{so.SONumber}' delivered! Stock deducted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var so = await _context.SalesOrders.FindAsync(id);
        if (so == null) return NotFound();

        so.Status = "Cancelled";
        so.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Order '{so.SONumber}' cancelled.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesOrderItems)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (so == null) return NotFound();
        return View(so);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var so = await _context.SalesOrders
            .Include(s => s.SalesOrderItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (so != null)
        {
            _context.SalesOrderItems.RemoveRange(so.SalesOrderItems);
            _context.SalesOrders.Remove(so);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sales Order deleted!";
        }
        return RedirectToAction(nameof(Index));
    }
}