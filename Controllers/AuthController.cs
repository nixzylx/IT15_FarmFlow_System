using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
    {
        // ✅ Find user in database
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            ViewBag.ErrorMessage = "Invalid username or password.";
            return View();
        }

        // ✅ Hybrid password verification
        bool isPasswordValid = false;

        try
        {
            if (user.PasswordHash.StartsWith("$2"))
            {
                // BCrypt hash
                isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            else
            {
                // Plain text (legacy)
                isPasswordValid = (user.PasswordHash == password);

                // Auto-upgrade to BCrypt on success
                if (isPasswordValid)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
        }
        catch
        {
            ViewBag.ErrorMessage = "Invalid username or password.";
            return View();
        }

        if (!isPasswordValid)
        {
            ViewBag.ErrorMessage = "Invalid username or password.";
            return View();
        }

        // ✅ Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "User"),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string firstName, string lastName, string email,
                                               string username, string password, string confirmPassword,
                                               int roleId)
    {
        if (password != confirmPassword)
        {
            ViewBag.ErrorMessage = "Passwords do not match!";
            return View();
        }

        if (password.Length < 6)
        {
            ViewBag.ErrorMessage = "Password must be at least 6 characters long!";
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            ViewBag.ErrorMessage = "Username already exists!";
            return View();
        }

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            ViewBag.ErrorMessage = "Email already exists!";
            return View();
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            RoleId = roleId,
            IsActive = true,
            CreatedDateTime = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Registration successful! Please login.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}