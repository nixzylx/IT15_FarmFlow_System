using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Purchaser")]
public class SupplierController : Controller
{
    private readonly ApplicationDbContext _context;

    public SupplierController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Supplier
    public async Task<IActionResult> Index(string searchTerm)
    {
        var query = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s => s.CompanyName.Contains(searchTerm) || s.SupplierCode.Contains(searchTerm));
        }

        var suppliers = await query.OrderBy(s => s.CompanyName).ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.TotalSuppliers = suppliers.Count;
        ViewBag.ActiveSuppliers = suppliers.Count(s => s.IsActive);
        ViewBag.AvgRating = suppliers.Any() ? suppliers.Average(s => s.PerformanceRating ?? 0) : 0;

        return View(suppliers);
    }

    // GET: /Supplier/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // GET: /Supplier/Create
    public IActionResult Create() => View(new Supplier());

    // POST: /Supplier/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (await _context.Suppliers.AnyAsync(s => s.SupplierCode == supplier.SupplierCode))
        {
            ModelState.AddModelError("SupplierCode", "Supplier Code already exists!");
        }

        if (ModelState.IsValid)
        {
            supplier.CreatedDateTime = DateTime.Now;
            supplier.IsActive = true;
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Supplier '{supplier.CompanyName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(supplier);
    }

    // GET: /Supplier/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Supplier supplier)
    {
        if (id != supplier.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.Suppliers.FindAsync(id);
            if (existing == null) return NotFound();

            existing.SupplierCode = supplier.SupplierCode;
            existing.CompanyName = supplier.CompanyName;
            existing.ContactPerson = supplier.ContactPerson;
            existing.ContactEmail = supplier.ContactEmail;
            existing.ContactPhone = supplier.ContactPhone;
            existing.Address = supplier.Address;
            existing.TaxId = supplier.TaxId;
            existing.PaymentTerms = supplier.PaymentTerms;
            existing.CreditLimit = supplier.CreditLimit;
            existing.PerformanceRating = supplier.PerformanceRating;
            existing.Notes = supplier.Notes;
            existing.IsActive = supplier.IsActive;
            existing.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Supplier updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(supplier);
    }

    // GET: /Supplier/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    // POST: /Supplier/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Supplier deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }
}