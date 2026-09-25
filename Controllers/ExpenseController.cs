using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Accountant")]
public class ExpenseController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpenseController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Expense
    public async Task<IActionResult> Index(string searchTerm, string status, int? categoryId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Farm)
            .Include(e => e.ProductionBatch)
            .Include(e => e.CreatedByUser)
            .Include(e => e.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(e =>
                e.ExpenseNumber.Contains(searchTerm) ||
                (e.Description != null && e.Description.Contains(searchTerm)) ||
                (e.Vendor != null && e.Vendor.Contains(searchTerm)) ||
                (e.ReferenceNumber != null && e.ReferenceNumber.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        if (categoryId.HasValue && categoryId > 0)
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);

        if (fromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= toDate.Value.Date.AddDays(1).AddSeconds(-1));

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.CategoryId = categoryId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        ViewBag.Categories = await _context.ExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.TotalCount = expenses.Count;
        ViewBag.PendingCount = expenses.Count(e => e.Status == "Pending");
        ViewBag.ApprovedCount = expenses.Count(e => e.Status == "Approved");
        ViewBag.TotalAmount = expenses.Sum(e => e.Amount);
        ViewBag.PendingAmount = expenses.Where(e => e.Status == "Pending").Sum(e => e.Amount);

        return View(expenses);
    }

    // GET: /Expense/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Farm)
            .Include(e => e.ProductionBatch)
            .Include(e => e.CreatedByUser)
            .Include(e => e.ApprovedByUser)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null) return NotFound();
        return View(expense);
    }

    // GET: /Expense/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new Expense
        {
            ExpenseNumber = await GenerateNextExpenseNumber(),
            ExpenseDate = DateTime.Now,
            Status = "Pending"
        });
    }

    // POST: /Expense/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Expense expense)
    {
        if (await _context.Expenses.AnyAsync(e => e.ExpenseNumber == expense.ExpenseNumber))
            ModelState.AddModelError(nameof(expense.ExpenseNumber), "Expense number already exists.");

        if (ModelState.IsValid)
        {
            expense.CreatedDateTime = DateTime.Now;

            // Auto-assign logged-in user as creator
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                expense.CreatedByUserId = userId;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Expense '{expense.ExpenseNumber}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(expense);
    }

    // GET: /Expense/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        // Block editing approved/paid expenses
        if (expense.Status == "Approved" || expense.Status == "Paid")
        {
            TempData["ErrorMessage"] = $"Cannot edit an expense with status '{expense.Status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateDropdowns();
        return View(expense);
    }

    // POST: /Expense/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Expense expense)
    {
        if (id != expense.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.Expenses.FindAsync(id);
            if (existing == null) return NotFound();

            if (existing.Status == "Approved" || existing.Status == "Paid")
            {
                TempData["ErrorMessage"] = $"Cannot edit an expense with status '{existing.Status}'.";
                return RedirectToAction(nameof(Details), new { id });
            }

            existing.ExpenseDate = expense.ExpenseDate;
            existing.ExpenseCategoryId = expense.ExpenseCategoryId;
            existing.Amount = expense.Amount;
            existing.Description = expense.Description;
            existing.Vendor = expense.Vendor;
            existing.ReferenceNumber = expense.ReferenceNumber;
            existing.FarmId = expense.FarmId;
            existing.ProductionBatchId = expense.ProductionBatchId;
            existing.Status = expense.Status;
            existing.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Expense updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(expense);
    }

    // POST: /Expense/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Approve(int id, string? approvalNotes)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        if (expense.Status != "Pending")
        {
            TempData["ErrorMessage"] = $"Only pending expenses can be approved. Current status: {expense.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            expense.ApprovedByUserId = userId;

        expense.Status = "Approved";
        expense.ApprovedDate = DateTime.Now;
        expense.ApprovalNotes = approvalNotes;
        expense.ModifiedDateTime = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Expense '{expense.ExpenseNumber}' approved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Expense/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reject(int id, string? approvalNotes)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        if (expense.Status != "Pending")
        {
            TempData["ErrorMessage"] = $"Only pending expenses can be rejected. Current status: {expense.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            expense.ApprovedByUserId = userId;

        expense.Status = "Rejected";
        expense.ApprovedDate = DateTime.Now;
        expense.ApprovalNotes = approvalNotes;
        expense.ModifiedDateTime = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Expense '{expense.ExpenseNumber}' rejected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Expense/MarkPaid/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        if (expense.Status != "Approved")
        {
            TempData["ErrorMessage"] = "Only approved expenses can be marked as paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        expense.Status = "Paid";
        expense.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Expense '{expense.ExpenseNumber}' marked as paid.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Expense/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.ExpenseCategory)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null) return NotFound();
        return View(expense);
    }

    // POST: /Expense/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var expense = await _context.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        if (expense.Status == "Approved" || expense.Status == "Paid")
        {
            TempData["ErrorMessage"] = $"Cannot delete an expense with status '{expense.Status}'. Reject it instead.";
            return RedirectToAction(nameof(Index));
        }

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Helpers
    private async Task PopulateDropdowns()
    {
        ViewBag.Categories = new SelectList(
            await _context.ExpenseCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(),
            "Id", "Name");

        ViewBag.Farms = new SelectList(
            await _context.Farms
                .Where(f => f.IsActive)
                .OrderBy(f => f.FarmName)
                .ToListAsync(),
            "Id", "FarmName");

        ViewBag.ProductionBatches = new SelectList(
            await _context.ProductionBatches
                .OrderByDescending(b => b.Id)
                .Take(50)
                .ToListAsync(),
            "Id", "BatchCode");
    }

    private async Task<string> GenerateNextExpenseNumber()
    {
        var last = await _context.Expenses
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        int next = (last?.Id ?? 0) + 1;
        return $"EXP-{DateTime.Now:yyyyMM}-{next:D4}";
    }
}