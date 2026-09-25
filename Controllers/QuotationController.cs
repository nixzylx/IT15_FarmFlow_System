using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Sales_Rep")]
public class QuotationController : Controller
{
    private readonly ApplicationDbContext _context;

    public QuotationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Quotation
    public async Task<IActionResult> Index(string searchTerm, string status)
    {
        var query = _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(q =>
                q.QuotationNumber.Contains(searchTerm) ||
                (q.Customer != null && q.Customer.CompanyName != null && q.Customer.CompanyName.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        var quotations = await query
            .OrderByDescending(q => q.QuotationDate)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.TotalQuotations = quotations.Count;
        ViewBag.DraftCount = quotations.Count(q => q.Status == "Draft");
        ViewBag.SentCount = quotations.Count(q => q.Status == "Sent");
        ViewBag.TotalValue = quotations
            .Where(q => q.Status == "Accepted" || q.Status == "Converted")
            .Sum(q => q.TotalAmount ?? 0);

        return View(quotations);
    }

    // GET: /Quotation/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null) return NotFound();
        return View(quotation);
    }

    // GET: /Quotation/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new Quotation
        {
            QuotationNumber = await GenerateNextQuotationNumber(),
            QuotationDate = DateTime.Now,
            ValidUntilDate = DateTime.Now.AddDays(30),
            Status = "Draft"
        });
    }

    // POST: /Quotation/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Quotation quotation)
    {
        if (await _context.Quotations.AnyAsync(q => q.QuotationNumber == quotation.QuotationNumber))
            ModelState.AddModelError(nameof(quotation.QuotationNumber), "Quotation Number already exists!");

        if (ModelState.IsValid)
        {
            quotation.CreatedDateTime = DateTime.Now;
            _context.Quotations.Add(quotation);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Quotation '{quotation.QuotationNumber}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(quotation);
    }

    // GET: /Quotation/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var quotation = await _context.Quotations.FindAsync(id);
        if (quotation == null) return NotFound();

        await PopulateDropdowns();
        return View(quotation);
    }

    // POST: /Quotation/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Quotation quotation)
    {
        if (id != quotation.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.Quotations.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CustomerId = quotation.CustomerId;
            existing.SalesRepId = quotation.SalesRepId;
            existing.QuotationDate = quotation.QuotationDate;
            existing.ValidUntilDate = quotation.ValidUntilDate;
            existing.Status = quotation.Status;
            existing.SubtotalAmount = quotation.SubtotalAmount;
            existing.TaxAmount = quotation.TaxAmount;
            existing.TotalAmount = quotation.TotalAmount;
            existing.Notes = quotation.Notes;
            existing.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Quotation updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(quotation);
    }

    // GET: /Quotation/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.SalesRep)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null) return NotFound();
        return View(quotation);
    }

    // POST: /Quotation/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var quotation = await _context.Quotations.FindAsync(id);
        if (quotation != null)
        {
            _context.Quotations.Remove(quotation);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Quotation deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Quotation/ConvertToSalesOrder/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToSalesOrder(int id)
    {
        var quotation = await _context.Quotations.FindAsync(id);
        if (quotation == null) return NotFound();

        if (quotation.ConvertedToSOId.HasValue)
        {
            TempData["ErrorMessage"] = "Quotation already converted to a Sales Order.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (quotation.Status == "Rejected" || quotation.Status == "Expired")
        {
            TempData["ErrorMessage"] = $"Cannot convert a {quotation.Status} quotation.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var salesOrder = new SalesOrder
        {
            SONumber = await GenerateNextSONumber(),
            CustomerId = quotation.CustomerId,
            OrderDate = DateTime.Now,
            Status = "Confirmed",
            PaymentStatus = "Unpaid",
            SubtotalAmount = quotation.SubtotalAmount,
            TaxAmount = quotation.TaxAmount,
            TotalAmount = quotation.TotalAmount,
            Notes = $"Converted from Quotation {quotation.QuotationNumber}",
            SalesRepId = quotation.SalesRepId,
            CreatedDateTime = DateTime.Now
        };

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        quotation.Status = "Converted";
        quotation.ConvertedToSOId = salesOrder.Id;
        quotation.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Quotation converted to Sales Order '{salesOrder.SONumber}'!";
        return RedirectToAction("Details", "SalesOrder", new { id = salesOrder.Id });
    }

    // Helpers
    private async Task PopulateDropdowns()
    {
        ViewBag.Customers = new SelectList(
            await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync(),
            "Id", "CompanyName");

        ViewBag.SalesReps = new SelectList(
            await _context.Users
                .Where(u => u.Role!.Name == "Sales_Rep" || u.Role.Name == "Administrator")
                .ToListAsync(),
            "Id", "FirstName");
    }

    private async Task<string> GenerateNextQuotationNumber()
    {
        var last = await _context.Quotations
            .OrderByDescending(q => q.Id)
            .FirstOrDefaultAsync();

        int next = (last?.Id ?? 0) + 1;
        return $"QT-{DateTime.Now:yyyyMM}-{next:D4}";
    }

    private async Task<string> GenerateNextSONumber()
    {
        var last = await _context.SalesOrders
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        int next = (last?.Id ?? 0) + 1;
        return $"SO-{DateTime.Now:yyyyMM}-{next:D4}";
    }
}