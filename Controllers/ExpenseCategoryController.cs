using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class ExpenseCategoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpenseCategoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /ExpenseCategory
    public async Task<IActionResult> Index(string searchTerm)
    {
        var query = _context.ExpenseCategories
            .Include(c => c.Expenses)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.Name.Contains(searchTerm) ||
                (c.Description != null && c.Description.Contains(searchTerm)));
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.TotalCategories = categories.Count;
        ViewBag.ActiveCategories = categories.Count(c => c.IsActive);
        ViewBag.TotalExpenses = categories.Sum(c => c.Expenses?.Count ?? 0);
        ViewBag.TotalExpenseAmount = categories.Sum(c => c.Expenses?.Sum(e => e.Amount) ?? 0);

        return View(categories);
    }

    // GET: /ExpenseCategory/Create
    public IActionResult Create()
    {
        return View(new ExpenseCategory());
    }

    // POST: /ExpenseCategory/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCategory category)
    {
        if (await _context.ExpenseCategories.AnyAsync(c => c.Name == category.Name))
            ModelState.AddModelError(nameof(category.Name), "Category name already exists.");

        if (ModelState.IsValid)
        {
            category.CreatedDateTime = DateTime.Now;
            _context.ExpenseCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(category);
    }

    // GET: /ExpenseCategory/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.ExpenseCategories.FindAsync(id);
        if (category == null) return NotFound();

        return View(category);
    }

    // POST: /ExpenseCategory/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseCategory category)
    {
        if (id != category.Id) return NotFound();

        if (await _context.ExpenseCategories.AnyAsync(c => c.Name == category.Name && c.Id != id))
            ModelState.AddModelError(nameof(category.Name), "Category name already exists.");

        if (ModelState.IsValid)
        {
            var existing = await _context.ExpenseCategories.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.IsActive = category.IsActive;
            existing.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(category);
    }

    // GET: /ExpenseCategory/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.ExpenseCategories
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();
        return View(category);
    }

    // POST: /ExpenseCategory/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.ExpenseCategories
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        // Prevent deletion if it has expenses
        if (category.Expenses != null && category.Expenses.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete '{category.Name}' — it has {category.Expenses.Count} expense(s) recorded. Deactivate it instead.";
            return RedirectToAction(nameof(Index));
        }

        _context.ExpenseCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Category deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}