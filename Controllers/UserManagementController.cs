using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class UserManagementController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserManagementController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================
    // INDEX: List users with search & role filter
    // ============================================
    public async Task<IActionResult> Index(string searchTerm, int? roleId)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u =>
                u.Username.Contains(searchTerm) ||
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm) ||
                u.Email.Contains(searchTerm));
        }

        // Role filter
        if (roleId.HasValue && roleId > 0)
        {
            query = query.Where(u => u.RoleId == roleId);
        }

        var users = await query.OrderBy(u => u.Id).ToListAsync();

        // Load roles for filter dropdown
        ViewBag.Roles = await _context.Roles.ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.RoleId = roleId;

        return View(users);
    }

    // ============================================
    // CREATE: Show form
    // ============================================
    public IActionResult Create()
    {
        LoadRoles();
        return View(new User());
    }

    // ============================================
    // CREATE: Save to database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user, string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            ModelState.AddModelError("password", "Password must be at least 6 characters long!");
            LoadRoles();
            return View(user);
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            ModelState.AddModelError("Username", "Username already exists!");
            LoadRoles();
            return View(user);
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            ModelState.AddModelError("Email", "Email already exists!");
            LoadRoles();
            return View(user);
        }

        try
        {
            // Hash the password with BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedDateTime = DateTime.Now;
            user.IsActive = true;

            _context.Add(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{user.Username}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error: {ex.Message}");
            LoadRoles();
            return View(user);
        }
    }

    // ============================================
    // EDIT: Show form
    // ============================================
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        LoadRoles();
        return View(user);
    }

    // ============================================
    // EDIT: Update database
    // ============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User user, string? password)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.MobileNumber = user.MobileNumber;
                existingUser.RoleId = user.RoleId;
                existingUser.IsActive = user.IsActive;
                existingUser.ModifiedDateTime = DateTime.Now;

                // Update password only if provided
                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Length < 6)
                    {
                        ModelState.AddModelError("password", "Password must be at least 6 characters long!");
                        LoadRoles();
                        return View(user);
                    }
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }

                _context.Update(existingUser);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User '{existingUser.Username}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }
        LoadRoles();
        return View(user);
    }

    // ============================================
    // DELETE: Show confirmation
    // ============================================
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // ============================================
    // DELETE: Remove from database
    // ============================================
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    // ============================================
    // Helper: Load roles into ViewBag
    // ============================================
    private void LoadRoles()
    {
        var roles = _context.Roles.ToList();

        // If no roles exist, create default ones
        if (roles == null || !roles.Any())
        {
            var defaultRoles = new List<Role>
            {
                new Role { Name = "Administrator", Description = "Full system access", CreatedDateTime = DateTime.Now },
                new Role { Name = "Production_Manager", Description = "Manage production and crops", CreatedDateTime = DateTime.Now },
                new Role { Name = "Warehouse_Keeper", Description = "Manage inventory", CreatedDateTime = DateTime.Now },
                new Role { Name = "Purchaser", Description = "Manage purchasing", CreatedDateTime = DateTime.Now },
                new Role { Name = "Sales_Rep", Description = "Manage sales and customers", CreatedDateTime = DateTime.Now },
                new Role { Name = "Accountant", Description = "Manage finances", CreatedDateTime = DateTime.Now }
            };

            _context.Roles.AddRange(defaultRoles);
            _context.SaveChanges();
            roles = _context.Roles.ToList();
        }

        ViewBag.Roles = roles;
    }
}