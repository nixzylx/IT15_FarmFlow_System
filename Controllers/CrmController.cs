using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Sales_Rep")]
public class CrmController : Controller
{
    private readonly ApplicationDbContext _context;

    public CrmController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Crm
    public async Task<IActionResult> Index(int? customerId)
    {
        var query = _context.CrmInteractions
            .Include(i => i.Customer)
            .Include(i => i.SalesRep)
            .AsQueryable();

        if (customerId.HasValue && customerId > 0)
            query = query.Where(i => i.CustomerId == customerId);

        var interactions = await query
            .OrderByDescending(i => i.InteractionDate)
            .ToListAsync();

        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        ViewBag.CustomerId = customerId;
        ViewBag.TotalInteractions = interactions.Count;
        ViewBag.OpenCount = interactions.Count(i => !i.IsResolved);
        ViewBag.ResolvedCount = interactions.Count(i => i.IsResolved);

        return View(interactions);
    }

    // GET: /Crm/Create
    public async Task<IActionResult> Create(int? customerId)
    {
        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        var model = new CrmInteraction
        {
            InteractionDate = DateTime.Now,
            InteractionType = "Call"
        };

        // Pre-select customer if navigated from Customer Details
        if (customerId.HasValue && customerId > 0)
            model.CustomerId = customerId.Value;

        return View(model);
    }

    // POST: /Crm/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrmInteraction interaction)
    {
        if (ModelState.IsValid)
        {
            interaction.CreatedDateTime = DateTime.Now;

            // Auto-assign logged-in user as sales rep
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                interaction.SalesRepId = userId;

            _context.CrmInteractions.Add(interaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Interaction logged successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return View(interaction);
    }

    // GET: /Crm/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var interaction = await _context.CrmInteractions.FindAsync(id);
        if (interaction == null) return NotFound();

        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return View(interaction);
    }

    // POST: /Crm/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CrmInteraction interaction)
    {
        if (id != interaction.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.CrmInteractions.FindAsync(id);
            if (existing == null) return NotFound();

            existing.InteractionType = interaction.InteractionType;
            existing.Subject = interaction.Subject;
            existing.Details = interaction.Details;
            existing.NextFollowUpDate = interaction.NextFollowUpDate;
            existing.IsResolved = interaction.IsResolved;
            existing.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Interaction updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        return View(interaction);
    }

    // GET: /Crm/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var interaction = await _context.CrmInteractions
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (interaction == null) return NotFound();
        return View(interaction);
    }

    // POST: /Crm/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var interaction = await _context.CrmInteractions.FindAsync(id);
        if (interaction != null)
        {
            _context.CrmInteractions.Remove(interaction);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Interaction deleted!";
        }
        return RedirectToAction(nameof(Index));
    }
}