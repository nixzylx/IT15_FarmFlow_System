using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Production_Manager")]
public class ProductionBatchController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductionBatchController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================
    // READ: List all batches
    // ============================================
    public async Task<IActionResult> Index()
    {
        var batches = await _context.ProductionBatches
            .Include(p => p.Farm)
            .Include(p => p.Crop)
            .OrderByDescending(p => p.CreatedDateTime)
            .ToListAsync();
        return View(batches);
    }

    // ============================================
    // CREATE: Show form
    // ============================================
    public IActionResult Create()
    {
        ViewBag.Farms = _context.Farms.Where(f => f.IsActive).ToList();
        ViewBag.Crops = _context.Crops.Where(c => c.IsActive).ToList();
        return View(new ProductionBatch());
    }

    // ============================================
    // CREATE: Save to database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionBatch batch)
    {
        // Validate BatchCode
        if (string.IsNullOrEmpty(batch.BatchCode))
        {
            ModelState.AddModelError("BatchCode", "Batch Code is required!");
        }
        else
        {
            var existing = await _context.ProductionBatches
                .FirstOrDefaultAsync(b => b.BatchCode == batch.BatchCode);
            if (existing != null)
            {
                ModelState.AddModelError("BatchCode", "Batch Code already exists!");
            }
        }

        // Validate Farm
        if (batch.FarmId <= 0)
        {
            ModelState.AddModelError("FarmId", "Please select a farm!");
        }

        // Validate Crop
        if (batch.CropId <= 0)
        {
            ModelState.AddModelError("CropId", "Please select a crop!");
        }

        // Validate Dates
        if (batch.PlantingDate.HasValue && batch.ExpectedHarvestDate.HasValue)
        {
            if (batch.ExpectedHarvestDate < batch.PlantingDate)
            {
                ModelState.AddModelError("ExpectedHarvestDate", "Harvest date must be after planting date!");
            }
        }

        if (ModelState.IsValid)
        {
            // Set default stage if not set
            if (string.IsNullOrEmpty(batch.CurrentStage))
            {
                batch.CurrentStage = "Planning";
            }

            batch.CreatedDateTime = DateTime.Now;
            batch.IsActive = true;

            _context.Add(batch);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"✅ Batch '{batch.BatchCode}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Reload dropdowns if validation fails
        ViewBag.Farms = _context.Farms.Where(f => f.IsActive).ToList();
        ViewBag.Crops = _context.Crops.Where(c => c.IsActive).ToList();
        return View(batch);
    }

    // ============================================
    // EDIT: Show form with data
    // ============================================
    public async Task<IActionResult> Edit(int id)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch == null)
        {
            return NotFound();
        }

        ViewBag.Farms = _context.Farms.Where(f => f.IsActive).ToList();
        ViewBag.Crops = _context.Crops.Where(c => c.IsActive).ToList();
        return View(batch);
    }

    // ============================================
    // EDIT: Update in database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductionBatch batch)
    {
        if (id != batch.Id)
        {
            return NotFound();
        }

        // Validate BatchCode
        if (string.IsNullOrEmpty(batch.BatchCode))
        {
            ModelState.AddModelError("BatchCode", "Batch Code is required!");
        }
        else
        {
            var existing = await _context.ProductionBatches
                .FirstOrDefaultAsync(b => b.BatchCode == batch.BatchCode && b.Id != batch.Id);
            if (existing != null)
            {
                ModelState.AddModelError("BatchCode", "Batch Code already exists!");
            }
        }

        // Validate Dates
        if (batch.PlantingDate.HasValue && batch.ExpectedHarvestDate.HasValue)
        {
            if (batch.ExpectedHarvestDate < batch.PlantingDate)
            {
                ModelState.AddModelError("ExpectedHarvestDate", "Harvest date must be after planting date!");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.ProductionBatches.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.BatchCode = batch.BatchCode;
                existing.FarmId = batch.FarmId;
                existing.CropId = batch.CropId;
                existing.FieldName = batch.FieldName;
                existing.FieldAreaHectares = batch.FieldAreaHectares;
                existing.PlantingDate = batch.PlantingDate;
                existing.ExpectedHarvestDate = batch.ExpectedHarvestDate;
                existing.ActualHarvestDate = batch.ActualHarvestDate;
                existing.CurrentStage = batch.CurrentStage;
                existing.CropHealthStatus = batch.CropHealthStatus;
                existing.PredictedYieldKg = batch.PredictedYieldKg;
                existing.ActualYieldKg = batch.ActualYieldKg;
                existing.IsActive = batch.IsActive;
                existing.ModifiedDateTime = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"✅ Batch '{existing.BatchCode}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BatchExists(batch.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        ViewBag.Farms = _context.Farms.Where(f => f.IsActive).ToList();
        ViewBag.Crops = _context.Crops.Where(c => c.IsActive).ToList();
        return View(batch);
    }

    // ============================================
    // DELETE: Show confirmation
    // ============================================
    public async Task<IActionResult> Delete(int id)
    {
        var batch = await _context.ProductionBatches
            .Include(p => p.Farm)
            .Include(p => p.Crop)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (batch == null)
        {
            return NotFound();
        }
        return View(batch);
    }

    // ============================================
    // DELETE: Remove from database
    // ============================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch != null)
        {
            _context.ProductionBatches.Remove(batch);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "✅ Batch deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ============================================
    // DETAILS: View single batch
    // ============================================
    public async Task<IActionResult> Details(int id)
    {
        var batch = await _context.ProductionBatches
            .Include(p => p.Farm)
            .Include(p => p.Crop)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (batch == null)
        {
            return NotFound();
        }
        return View(batch);
    }

    private bool BatchExists(int id)
    {
        return _context.ProductionBatches.Any(e => e.Id == id);
    }
}