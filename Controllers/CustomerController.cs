using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Sales_Rep")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomerController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm, string customerType)
    {
        var query = _context.Customers
            .Include(c => c.AssignedSalesRep)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.CustomerCode.Contains(searchTerm) ||
                c.CompanyName!.Contains(searchTerm) ||
                c.FirstName!.Contains(searchTerm) ||
                c.LastName!.Contains(searchTerm) ||
                c.Email!.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(customerType))
            query = query.Where(c => c.CustomerType == customerType);

        var customers = await query.OrderBy(c => c.CompanyName).ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.CustomerType = customerType;
        ViewBag.TotalCustomers = customers.Count;
        ViewBag.ActiveCustomers = customers.Count(c => c.IsActive);
        ViewBag.TotalReceivable = customers.Sum(c => c.CurrentBalance ?? 0);

        return View(customers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.AssignedSalesRep)
            .Include(c => c.SalesOrders)
            .Include(c => c.CrmInteractions)
            .Include(c => c.Quotations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null) return NotFound();
        return View(customer);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.SalesReps = await _context.Users
            .Where(u => u.Role!.Name == "Sales_Rep" || u.Role.Name == "Administrator")
            .ToListAsync();
        return View(new Customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (await _context.Customers.AnyAsync(c => c.CustomerCode == customer.CustomerCode))
            ModelState.AddModelError("CustomerCode", "Customer Code already exists!");

        if (ModelState.IsValid)
        {
            customer.CreatedDateTime = DateTime.Now;
            customer.IsActive = true;
            customer.CurrentBalance = 0;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Customer '{customer.CompanyName ?? customer.FirstName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SalesReps = await _context.Users
            .Where(u => u.Role!.Name == "Sales_Rep" || u.Role.Name == "Administrator")
            .ToListAsync();
        return View(customer);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        ViewBag.SalesReps = await _context.Users
            .Where(u => u.Role!.Name == "Sales_Rep" || u.Role.Name == "Administrator")
            .ToListAsync();
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = await _context.Customers.FindAsync(id);
            if (existing == null) return NotFound();

            existing.CustomerType = customer.CustomerType;
            existing.CompanyName = customer.CompanyName;
            existing.FirstName = customer.FirstName;
            existing.LastName = customer.LastName;
            existing.Email = customer.Email;
            existing.PhoneNumber = customer.PhoneNumber;
            existing.Address = customer.Address;
            existing.TaxId = customer.TaxId;
            existing.CreditLimit = customer.CreditLimit;
            existing.PaymentTerms = customer.PaymentTerms;
            existing.DiscountTier = customer.DiscountTier;
            existing.AssignedSalesRepId = customer.AssignedSalesRepId;
            existing.Notes = customer.Notes;
            existing.IsActive = customer.IsActive;
            existing.ModifiedDateTime = DateTime.Now;

            _context.Update(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Customer updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SalesReps = await _context.Users
            .Where(u => u.Role!.Name == "Sales_Rep" || u.Role.Name == "Administrator")
            .ToListAsync();
        return View(customer);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.SalesOrders)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Customer deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }
}