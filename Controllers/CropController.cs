using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Production_Manager")]
public class CropController : Controller
{
    private readonly ApplicationDbContext _context;

    public CropController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Crop
    public async Task<IActionResult> Index()
    {
        var crops = await _context.Crops.ToListAsync();
        return View(crops);
    }

    // GET: /Crop/Create
    public IActionResult Create()
    {
        return View(new Crop());
    }

    // POST: /Crop/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Crop crop)
    {
        if (ModelState.IsValid)
        {
            // Check if CropCode already exists
            if (!string.IsNullOrEmpty(crop.CropCode))
            {
                var existing = await _context.Crops
                    .FirstOrDefaultAsync(c => c.CropCode == crop.CropCode);
                if (existing != null)
                {
                    ModelState.AddModelError("CropCode", "Crop Code already exists!");
                    return View(crop);
                }
            }

            crop.CreatedDateTime = DateTime.Now;
            crop.IsActive = true;

            _context.Add(crop);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Crop '{crop.CropName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(crop);
    }

    // GET: /Crop/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var crop = await _context.Crops.FindAsync(id);
        if (crop == null)
        {
            return NotFound();
        }
        return View(crop);
    }

    // POST: /Crop/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Crop crop)
    {
        if (id != crop.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Crops.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.CropName = crop.CropName;
                existing.Variety = crop.Variety;
                existing.CropCode = crop.CropCode;
                existing.Category = crop.Category;
                existing.GrowthDurationDays = crop.GrowthDurationDays;
                existing.ExpectedYieldPerHectare = crop.ExpectedYieldPerHectare;
                existing.UnitOfMeasure = crop.UnitOfMeasure;
                existing.Season = crop.Season;
                existing.IsActive = crop.IsActive;
                existing.ModifiedDateTime = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Crop '{existing.CropName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CropExists(crop.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }
        return View(crop);
    }

    // GET: /Crop/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var crop = await _context.Crops.FindAsync(id);
        if (crop == null)
        {
            return NotFound();
        }
        return View(crop);
    }

    // POST: /Crop/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var crop = await _context.Crops.FindAsync(id);
        if (crop != null)
        {
            _context.Crops.Remove(crop);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Crop deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CropExists(int id)
    {
        return _context.Crops.Any(e => e.Id == id);
    }
}