using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Warehouse_Keeper")]
public class InventoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public InventoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================
    // INDEX: List all inventory items
    // ============================================
    public async Task<IActionResult> Index(string searchTerm, int? categoryId)
    {
        var query = _context.InventoryItems
            .Include(i => i.Category)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(i => 
                i.ItemName.Contains(searchTerm) || 
                i.ItemCode.Contains(searchTerm));
        }

        // Category filter
        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }

        var items = await query.OrderBy(i => i.ItemName).ToListAsync();

        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CategoryId = categoryId;

        // Stats
        ViewBag.TotalItems = items.Count;
        ViewBag.LowStockItems = items.Count(i => i.CurrentStock <= i.ReorderLevel);
        ViewBag.TotalValue = items.Sum(i => i.CurrentStock * (i.CostPerUnit ?? 0));

        return View(items);
    }

    // ============================================
    // DETAILS: View single item
    // ============================================
    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return NotFound();

        // Get transaction history
        var transactions = await _context.InventoryTransactions
            .Include(t => t.PerformedByUser)
            .Where(t => t.InventoryItemId == id)
            .OrderByDescending(t => t.TransactionDateTime)
            .Take(20)
            .ToListAsync();

        ViewBag.Transactions = transactions;

        return View(item);
    }

    // ============================================
    // CREATE: Show form
    // ============================================
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).ToListAsync();
        return View(new InventoryItem());
    }

    // ============================================
    // CREATE: Save to database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem item)
    {
        // Check duplicate item code
        if (await _context.InventoryItems.AnyAsync(i => i.ItemCode == item.ItemCode))
        {
            ModelState.AddModelError("ItemCode", "Item Code already exists!");
        }

        if (ModelState.IsValid)
        {
            item.CreatedDateTime = DateTime.Now;
            item.IsActive = true;

            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Item '{item.ItemName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).ToListAsync();
        return View(item);
    }

    // ============================================
    // EDIT: Show form
    // ============================================
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();

        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).ToListAsync();
        return View(item);
    }

    // ============================================
    // EDIT: Update database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryItem item)
    {
        if (id != item.Id) return NotFound();

        // Check duplicate item code
        if (await _context.InventoryItems.AnyAsync(i => i.ItemCode == item.ItemCode && i.Id != id))
        {
            ModelState.AddModelError("ItemCode", "Item Code already exists!");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.InventoryItems.FindAsync(id);
                if (existing == null) return NotFound();

                existing.ItemCode = item.ItemCode;
                existing.ItemName = item.ItemName;
                existing.ItemType = item.ItemType;
                existing.CategoryId = item.CategoryId;
                existing.UnitOfMeasure = item.UnitOfMeasure;
                existing.ReorderLevel = item.ReorderLevel;
                existing.ReorderQuantity = item.ReorderQuantity;
                existing.CostPerUnit = item.CostPerUnit;
                existing.SellingPricePerUnit = item.SellingPricePerUnit;
                existing.StorageLocation = item.StorageLocation;
                existing.ExpiryDate = item.ExpiryDate;
                existing.IsActive = item.IsActive;
                existing.ModifiedDateTime = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Item '{existing.ItemName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(item.Id)) return NotFound();
                throw;
            }
        }

        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Locations = await _context.WarehouseLocations.Where(l => l.IsActive).ToListAsync();
        return View(item);
    }

    // ============================================
    // DELETE: Show confirmation
    // ============================================
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    // ============================================
    // DELETE: Remove from database
    // ============================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item != null)
        {
            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Item deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ============================================
    // ADJUST STOCK: Add/Remove/Transfer
    // ============================================
    public async Task<IActionResult> AdjustStock(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();

        ViewBag.Item = item;
        return View(new InventoryTransaction { InventoryItemId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(InventoryTransaction transaction)
    {
        var item = await _context.InventoryItems.FindAsync(transaction.InventoryItemId);
        if (item == null) return NotFound();

        if (ModelState.IsValid)
        {
            // Capture stock before
            transaction.StockBefore = item.CurrentStock;

            // Calculate new stock
            decimal newStock = item.CurrentStock + transaction.QuantityChange;
            if (newStock < 0)
            {
                ModelState.AddModelError("QuantityChange", "Stock cannot be negative!");
                ViewBag.Item = item;
                return View(transaction);
            }

            // Update item stock
            item.CurrentStock = newStock;
            item.ModifiedDateTime = DateTime.Now;
            transaction.StockAfter = newStock;

            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                transaction.PerformedByUserId = userId;
            }

            transaction.TransactionDateTime = DateTime.Now;

            _context.InventoryTransactions.Add(transaction);
            _context.Update(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Stock adjusted for '{item.ItemName}'. New stock: {newStock} {item.UnitOfMeasure}";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Item = item;
        return View(transaction);
    }

    private bool ItemExists(int id)
    {
        return _context.InventoryItems.Any(e => e.Id == id);
    }
}