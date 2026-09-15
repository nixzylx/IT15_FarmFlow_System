using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Production_Manager")]
public class FarmController : Controller
{
    private readonly ApplicationDbContext _context;

    public FarmController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Farm
    public async Task<IActionResult> Index()
    {
        var farms = await _context.Farms.ToListAsync();
        return View(farms);
    }

    // GET: /Farm/Create
    public IActionResult Create()
    {
        return View(new Farm());
    }

    // POST: /Farm/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Farm farm)
    {
        if (ModelState.IsValid)
        {
            // Check if FarmCode already exists
            if (!string.IsNullOrEmpty(farm.FarmCode))
            {
                var existingFarm = await _context.Farms
                    .FirstOrDefaultAsync(f => f.FarmCode == farm.FarmCode);
                if (existingFarm != null)
                {
                    ModelState.AddModelError("FarmCode", "Farm Code already exists!");
                    return View(farm);
                }
            }

            farm.CreatedDateTime = DateTime.Now;
            farm.IsActive = true;

            _context.Add(farm);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Farm '{farm.FarmName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(farm);
    }

    // GET: /Farm/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm == null)
        {
            return NotFound();
        }
        return View(farm);
    }

    // POST: /Farm/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Farm farm)
    {
        if (id != farm.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingFarm = await _context.Farms.FindAsync(id);
                if (existingFarm == null)
                {
                    return NotFound();
                }

                existingFarm.FarmName = farm.FarmName;
                existingFarm.FarmCode = farm.FarmCode;
                existingFarm.Address = farm.Address;
                existingFarm.City = farm.City;
                existingFarm.Province = farm.Province;
                existingFarm.TotalHectares = farm.TotalHectares;
                existingFarm.FarmType = farm.FarmType;
                existingFarm.SoilType = farm.SoilType;
                existingFarm.IsActive = farm.IsActive;
                existingFarm.ModifiedDateTime = DateTime.Now;

                _context.Update(existingFarm);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Farm '{existingFarm.FarmName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FarmExists(farm.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }
        return View(farm);
    }

    // GET: /Farm/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm == null)
        {
            return NotFound();
        }
        return View(farm);
    }

    // POST: /Farm/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var farm = await _context.Farms.FindAsync(id);
        if (farm != null)
        {
            _context.Farms.Remove(farm);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Farm deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool FarmExists(int id)
    {
        return _context.Farms.Any(e => e.Id == id);
    }
}