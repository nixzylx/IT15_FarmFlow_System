using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Accountant, Sales_Rep, Warehouse_Keeper, Production_Manager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =====================================================================
    // GET: /Reports
    // Dashboard: KPI summary across all modules
    // =====================================================================
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Now.AddMonths(-1);
        var to = toDate ?? DateTime.Now;

        // ─── SALES ────────────────────────────────────────
        var salesOrders = await _context.SalesOrders
            .Where(s => s.OrderDate >= from && s.OrderDate <= to)
            .ToListAsync();

        ViewBag.TotalSales = salesOrders.Sum(s => s.TotalAmount ?? 0);
        ViewBag.OrderCount = salesOrders.Count;
        ViewBag.AvgOrderValue = salesOrders.Any()
            ? salesOrders.Average(s => s.TotalAmount ?? 0)
            : 0;

        // ─── PAYMENTS (Customer received) ─────────────────
        var customerPayments = await _context.Payments
            .Where(p => p.PaymentType == "Customer"
                     && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        ViewBag.TotalReceived = customerPayments.Sum(p => p.Amount);

        // ─── OUTSTANDING (All-time, not range-based) ──────
        var allOrders = await _context.SalesOrders
            .Where(s => s.Status != "Cancelled")
            .ToListAsync();

        var allPayments = await _context.Payments
            .Where(p => p.PaymentType == "Customer")
            .ToListAsync();

        decimal totalBilled = allOrders.Sum(o => o.TotalAmount ?? 0);
        decimal totalPaid = allPayments.Sum(p => p.Amount);
        ViewBag.Outstanding = totalBilled - totalPaid;

        // ─── EXPENSES ─────────────────────────────────────
        var expenses = await _context.Expenses
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync();

        ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

        // ─── SUPPLIER PAYMENTS ────────────────────────────
        var supplierPayments = await _context.Payments
            .Where(p => p.PaymentType == "Supplier"
                     && p.PaymentDate >= from && p.PaymentDate <= to)
            .ToListAsync();

        ViewBag.TotalSupplierPaid = supplierPayments.Sum(p => p.Amount);

        // ─── NET CASH FLOW ────────────────────────────────
        ViewBag.NetCashFlow = customerPayments.Sum(p => p.Amount)
                            - supplierPayments.Sum(p => p.Amount)
                            - expenses.Sum(e => e.Amount);

        // ─── INVENTORY ────────────────────────────────────
        var inventory = await _context.InventoryItems
            .Where(i => i.IsActive)
            .ToListAsync();

        ViewBag.InventoryValue = inventory.Sum(i => i.CurrentStock * (i.CostPerUnit ?? 0));
        ViewBag.LowStockCount = inventory.Count(i =>
            i.ReorderLevel.HasValue && i.CurrentStock <= i.ReorderLevel.Value);

        // ─── CHART DATA — Monthly Sales (last 6 months) ───
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var monthlySales = await _context.SalesOrders
            .Where(s => s.OrderDate >= sixMonthsAgo)
            .GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(s => s.TotalAmount ?? 0)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        ViewBag.MonthlySalesLabels = monthlySales
            .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"))
            .ToList();
        ViewBag.MonthlySalesValues = monthlySales.Select(m => m.Total).ToList();

        // ─── CHART DATA — Expenses by Category ────────────
        var expensesByCategory = await _context.Expenses
            .Where(e => e.ExpenseDate >= sixMonthsAgo)
            .Include(e => e.ExpenseCategory)
            .GroupBy(e => e.ExpenseCategory!.Name)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();

        ViewBag.ExpenseCategoryLabels = expensesByCategory.Select(e => e.Category).ToList();
        ViewBag.ExpenseCategoryValues = expensesByCategory.Select(e => e.Total).ToList();

        ViewBag.FromDate = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate = to.ToString("yyyy-MM-dd");

        return View();
    }

    // =====================================================================
    // GET: /Reports/Sales
    // =====================================================================
    public async Task<IActionResult> Sales(DateTime? fromDate, DateTime? toDate, int? customerId)
    {
        var from = fromDate ?? DateTime.Now.AddMonths(-1);
        var to = toDate ?? DateTime.Now;

        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesRep)
            .Where(s => s.OrderDate >= from && s.OrderDate <= to);

        if (customerId.HasValue && customerId > 0)
            query = query.Where(s => s.CustomerId == customerId.Value);

        var orders = await query.OrderByDescending(s => s.OrderDate).ToListAsync();

        ViewBag.TotalSales = orders.Sum(s => s.TotalAmount ?? 0);
        ViewBag.OrderCount = orders.Count;
        ViewBag.AvgOrderValue = orders.Any() ? orders.Average(s => s.TotalAmount ?? 0) : 0;
        ViewBag.FromDate = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate = to.ToString("yyyy-MM-dd");
        ViewBag.CustomerId = customerId;

        ViewBag.Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();

        ViewBag.Orders = orders;

        // Chart — top 5 customers by revenue in range
        var topCustomers = orders
            .GroupBy(o => o.Customer?.CompanyName ?? "Unknown")
            .Select(g => new { Name = g.Key, Total = g.Sum(o => o.TotalAmount ?? 0) })
            .OrderByDescending(g => g.Total)
            .Take(5)
            .ToList();

        ViewBag.TopCustomerLabels = topCustomers.Select(c => c.Name).ToList();
        ViewBag.TopCustomerValues = topCustomers.Select(c => c.Total).ToList();

        return View();
    }

    // =====================================================================
    // GET: /Reports/Inventory
    // =====================================================================
    public async Task<IActionResult> Inventory(int? categoryId)
    {
        var query = _context.InventoryItems
            .Include(i => i.Category)
            .Where(i => i.IsActive);

        if (categoryId.HasValue && categoryId > 0)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        var items = await query.OrderBy(i => i.ItemName).ToListAsync();

        ViewBag.TotalItems = items.Count;
        ViewBag.TotalValue = items.Sum(i => i.CurrentStock * (i.CostPerUnit ?? 0));
        ViewBag.LowStockCount = items.Count(i =>
            i.ReorderLevel.HasValue && i.CurrentStock <= i.ReorderLevel.Value);
        ViewBag.OutOfStockCount = items.Count(i => i.CurrentStock <= 0);
        ViewBag.CategoryId = categoryId;

        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Items = items;

        // Chart — stock value by category
        var byCategory = items
            .GroupBy(i => i.Category?.Name ?? "Uncategorized")
            .Select(g => new
            {
                Name = g.Key,
                Value = g.Sum(i => i.CurrentStock * (i.CostPerUnit ?? 0))
            })
            .OrderByDescending(g => g.Value)
            .ToList();

        ViewBag.CategoryLabels = byCategory.Select(c => c.Name).ToList();
        ViewBag.CategoryValues = byCategory.Select(c => c.Value).ToList();

        return View();
    }

    // =====================================================================
    // GET: /Reports/Expenses
    // =====================================================================
    public async Task<IActionResult> Expenses(DateTime? fromDate, DateTime? toDate, int? categoryId)
    {
        var from = fromDate ?? DateTime.Now.AddMonths(-1);
        var to = toDate ?? DateTime.Now;

        var query = _context.Expenses
            .Include(e => e.ExpenseCategory)
            .Include(e => e.Farm)
            .Include(e => e.CreatedByUser)
            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to);

        if (categoryId.HasValue && categoryId > 0)
            query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);

        var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();

        ViewBag.TotalAmount = expenses.Sum(e => e.Amount);
        ViewBag.ExpenseCount = expenses.Count;
        ViewBag.AvgExpense = expenses.Any() ? expenses.Average(e => e.Amount) : 0;
        ViewBag.FromDate = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate = to.ToString("yyyy-MM-dd");
        ViewBag.CategoryId = categoryId;

        ViewBag.Categories = await _context.ExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Expenses = expenses;

        // Chart — expenses by category
        var byCategory = expenses
            .GroupBy(e => e.ExpenseCategory?.Name ?? "Uncategorized")
            .Select(g => new { Name = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .ToList();

        ViewBag.CategoryLabels = byCategory.Select(c => c.Name).ToList();
        ViewBag.CategoryValues = byCategory.Select(c => c.Total).ToList();

        // Chart — monthly trend
        var monthly = expenses
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(e => e.Amount)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToList();

        ViewBag.MonthlyLabels = monthly
            .Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"))
            .ToList();
        ViewBag.MonthlyValues = monthly.Select(m => m.Total).ToList();

        return View();
    }

    // =====================================================================
    // GET: /Reports/Customers
    // =====================================================================
    public async Task<IActionResult> Customers()
    {
        var customers = await _context.Customers
            .Include(c => c.SalesOrders)
            .Where(c => c.IsActive)
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.PaymentType == "Customer")
            .ToListAsync();

        // Build customer performance metrics
        var performance = customers.Select(c =>
        {
            var orders = c.SalesOrders?.Where(o => o.Status != "Cancelled").ToList() ?? new List<SalesOrder>();
            decimal billed = orders.Sum(o => o.TotalAmount ?? 0);
            decimal paid = payments
                .Where(p => orders.Any(o => o.Id == p.SalesOrderId))
                .Sum(p => p.Amount);

            return new
            {
                CustomerId = c.Id,
                CompanyName = c.CompanyName ?? $"{c.FirstName} {c.LastName}",
                CustomerType = c.CustomerType ?? "-",
                OrderCount = orders.Count,
                TotalBilled = billed,
                TotalPaid = paid,
                Outstanding = billed - paid,
                CreditLimit = c.CreditLimit ?? 0,
                CreditUtilization = c.CreditLimit.HasValue && c.CreditLimit.Value > 0
                    ? (billed - paid) / c.CreditLimit.Value * 100
                    : 0
            };
        })
        .OrderByDescending(p => p.TotalBilled)
        .ToList();

        ViewBag.Performance = performance;
        ViewBag.TotalBilled = performance.Sum(p => p.TotalBilled);
        ViewBag.TotalPaid = performance.Sum(p => p.TotalPaid);
        ViewBag.TotalOutstanding = performance.Sum(p => p.Outstanding);

        // Chart data
        ViewBag.TopCustomerLabels = performance.Take(5).Select(p => p.CompanyName).ToList();
        ViewBag.TopCustomerValues = performance.Take(5).Select(p => p.TotalBilled).ToList();

        return View();
    }
}