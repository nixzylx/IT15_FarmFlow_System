using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Data;
using FarmFlow.Web.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FarmFlow.Web.Controllers;

[Authorize(Roles = "Administrator, Accountant")]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentController(ApplicationDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    // =====================================================================
    // EXISTING METHODS (unchanged)
    // =====================================================================

    // GET: /Payment
    public async Task<IActionResult> Index(string searchTerm, string paymentType, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Payments
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .Include(p => p.PurchaseOrder).ThenInclude(po => po!.Supplier)
            .Include(p => p.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p =>
                p.PaymentNumber.Contains(searchTerm) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.Contains(searchTerm)) ||
                (p.Notes != null && p.Notes.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(paymentType))
            query = query.Where(p => p.PaymentType == paymentType);

        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDate <= toDate.Value.Date.AddDays(1).AddSeconds(-1));

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.PaymentType = paymentType;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        ViewBag.TotalCount = payments.Count;
        ViewBag.CustomerTotal = payments.Where(p => p.PaymentType == "Customer").Sum(p => p.Amount);
        ViewBag.SupplierTotal = payments.Where(p => p.PaymentType == "Supplier").Sum(p => p.Amount);
        ViewBag.TotalAmount = payments.Sum(p => p.Amount);

        return View(payments);
    }

    // GET: /Payment/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .Include(p => p.PurchaseOrder).ThenInclude(po => po!.Supplier)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        return View(payment);
    }

    // GET: /Payment/Create
    public async Task<IActionResult> Create(string? type)
    {
        await PopulateDropdowns(type);

        return View(new Payment
        {
            PaymentNumber = await GenerateNextPaymentNumber(),
            PaymentDate = DateTime.Now,
            PaymentType = string.IsNullOrEmpty(type) ? "Customer" : type
        });
    }

    // POST: /Payment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Payment payment)
    {
        if (await _context.Payments.AnyAsync(p => p.PaymentNumber == payment.PaymentNumber))
            ModelState.AddModelError(nameof(payment.PaymentNumber), "Payment number already exists.");

        if (payment.PaymentType == "Customer" && !payment.SalesOrderId.HasValue)
            ModelState.AddModelError(nameof(payment.SalesOrderId), "Please select a Sales Order for a customer payment.");

        if (payment.PaymentType == "Supplier" && !payment.PurchaseOrderId.HasValue)
            ModelState.AddModelError(nameof(payment.PurchaseOrderId), "Please select a Purchase Order for a supplier payment.");

        if (ModelState.IsValid)
        {
            payment.CreatedDateTime = DateTime.Now;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                payment.CreatedByUserId = userId;

            if (payment.PaymentType == "Customer")
                payment.PurchaseOrderId = null;
            else
                payment.SalesOrderId = null;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            if (payment.PaymentType == "Customer" && payment.SalesOrderId.HasValue)
                await UpdateSalesOrderPaymentStatus(payment.SalesOrderId.Value);

            TempData["SuccessMessage"] = $"Payment '{payment.PaymentNumber}' recorded successfully!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns(payment.PaymentType);
        return View(payment);
    }

    // GET: /Payment/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.SalesOrder).ThenInclude(so => so!.Customer)
            .Include(p => p.PurchaseOrder).ThenInclude(po => po!.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        return View(payment);
    }

    // POST: /Payment/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        var salesOrderId = payment.SalesOrderId;
        var paymentType = payment.PaymentType;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        if (paymentType == "Customer" && salesOrderId.HasValue)
            await UpdateSalesOrderPaymentStatus(salesOrderId.Value);

        TempData["SuccessMessage"] = "Payment deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // =====================================================================
    // ✅ NEW: PAYMONGO ONLINE PAYMENT
    // =====================================================================

    /// <summary>
    /// GET: /Payment/PayOnline/5
    /// Shows a confirmation page with amount + Pay button.
    /// </summary>
    public async Task<IActionResult> PayOnline(int id)
    {
        var salesOrder = await _context.SalesOrders
            .Include(so => so.Customer)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (salesOrder == null) return NotFound();

        if (salesOrder.PaymentStatus == "Paid")
        {
            TempData["ErrorMessage"] = "This order is already fully paid.";
            return RedirectToAction("Details", "SalesOrder", new { id });
        }

        // Compute balance due = total - already paid
        var totalPaid = await _context.Payments
            .Where(p => p.PaymentType == "Customer" && p.SalesOrderId == id)
            .SumAsync(p => p.Amount);

        ViewBag.BalanceDue = (salesOrder.TotalAmount ?? 0) - totalPaid;

        return View(salesOrder);
    }

    /// <summary>
    /// POST: /Payment/CreateOnlinePayment
    /// Creates a PayMongo Checkout Session and redirects to the hosted page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOnlinePayment(int salesOrderId, decimal amount)
    {
        var salesOrder = await _context.SalesOrders
            .Include(so => so.Customer)
            .FirstOrDefaultAsync(so => so.Id == salesOrderId);

        if (salesOrder == null) return NotFound();

        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Payment amount must be greater than zero.";
            return RedirectToAction(nameof(PayOnline), new { id = salesOrderId });
        }

        var secretKey = _config["PayMongo:SecretKey"];
        var publicUrl = _config["PayMongo:PublicUrl"];

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(publicUrl))
        {
            TempData["ErrorMessage"] = "PayMongo is not configured. Please check appsettings.json.";
            return RedirectToAction(nameof(PayOnline), new { id = salesOrderId });
        }

        // Reference number format: SO-{id}-{guid-short}  → used to reconcile the webhook
        string referenceNumber = $"SO-{salesOrder.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

        // Build the Checkout Session payload
        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    send_email_receipt = true,
                    show_description = true,
                    show_line_items = true,
                    description = $"Payment for Sales Order {salesOrder.SONumber}",
                    line_items = new[]
                    {
                        new
                        {
                            currency = "PHP",
                            amount = (long)(amount * 100), // PayMongo expects centavos
                            name = $"SO {salesOrder.SONumber}",
                            quantity = 1
                        }
                    },
                    payment_method_types = new[] { "gcash", "paymaya", "card", "dob" }, // GCash, Maya, Card, Direct Online Banking
                    success_url = $"{publicUrl}/Payment/PaymentSuccess?ref={referenceNumber}",
                    cancel_url = $"{publicUrl}/Payment/PaymentCancelled?ref={referenceNumber}",
                    reference_number = referenceNumber
                }
            }
        };

        // Call PayMongo API
         var client = _httpClientFactory.CreateClient();
    var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Change v1 → v2 in this URL:
    var response = await client.PostAsync("https://api.paymongo.com/v2/checkout_sessions", content);
    var responseBody = await response.Content.ReadAsStringAsync();
    
        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = $"PayMongo error: {responseBody}";
            return RedirectToAction(nameof(PayOnline), new { id = salesOrderId });
        }

        // Parse response to get session ID + checkout URL
        using var doc = JsonDocument.Parse(responseBody);
        var dataEl = doc.RootElement.GetProperty("data");
        var sessionId = dataEl.GetProperty("id").GetString();
        var checkoutUrl = dataEl.GetProperty("attributes").GetProperty("checkout_url").GetString();

        // Persist a pending Payment record so we can reconcile later
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out int userId);

        var payment = new Payment
        {
            PaymentNumber = await GenerateNextPaymentNumber(),
            PaymentDate = DateTime.Now,
            PaymentType = "Customer",
            SalesOrderId = salesOrderId,
            Amount = amount,
            PaymentMethod = PaymentMethod.Other, // Placeholder; updated on webhook
            ReferenceNumber = referenceNumber,
            Notes = "PayMongo online checkout (pending)",
            PayMongoCheckoutSessionId = sessionId,
            CreatedByUserId = userId > 0 ? userId : 1,
            CreatedDateTime = DateTime.Now
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Redirect customer to PayMongo hosted page
        return Redirect(checkoutUrl!);
    }

    /// <summary>
    /// GET: /Payment/PaymentSuccess
    /// PayMongo redirects here after successful payment. The webhook does the real update.
    /// </summary>
    [AllowAnonymous]
    public IActionResult PaymentSuccess(string? @ref)
    {
        ViewBag.Reference = @ref;
        return View();
    }

    /// <summary>
    /// GET: /Payment/PaymentCancelled
    /// PayMongo redirects here if the customer cancels.
    /// </summary>
    [AllowAnonymous]
    public IActionResult PaymentCancelled(string? @ref)
    {
        ViewBag.Reference = @ref;
        return View();
    }

    /// <summary>
    /// POST: /Payment/Webhook
    /// Receives PayMongo events. Verifies signature, then reconciles the payment.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        // Read raw body for signature verification
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        var signatureHeader = Request.Headers["Paymongo-Signature"].ToString();
        var webhookSecret = _config["PayMongo:WebhookSigningSecret"];

        if (string.IsNullOrEmpty(webhookSecret) || string.IsNullOrEmpty(signatureHeader))
            return Unauthorized();

        if (!VerifyPayMongoSignature(rawBody, signatureHeader, webhookSecret))
            return Unauthorized();

        // Parse event
        using var doc = JsonDocument.Parse(rawBody);
        var eventType = doc.RootElement
            .GetProperty("data").GetProperty("attributes")
            .GetProperty("type").GetString();

        if (eventType == "checkout_session.payment.paid")
        {
            // Navigate to the actual payment resource
            var sessionAttrs = doc.RootElement
                .GetProperty("data").GetProperty("attributes")
                .GetProperty("data").GetProperty("attributes");

            string? referenceNumber = sessionAttrs.TryGetProperty("reference_number", out var refEl)
                ? refEl.GetString()
                : null;

            string? paymentIntentId = sessionAttrs.TryGetProperty("payment_intent", out var piEl)
                && piEl.TryGetProperty("id", out var piIdEl)
                ? piIdEl.GetString()
                : null;

            string? channel = sessionAttrs.TryGetProperty("payment_method_used", out var chEl)
                ? chEl.GetString()
                : null;

            if (string.IsNullOrEmpty(referenceNumber))
                return Ok(); // nothing to reconcile

            // Find the pending Payment by ReferenceNumber
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber
                                          && p.PayMongoCheckoutSessionId != null);

            if (payment == null)
                return Ok(); // already reconciled or unknown

            payment.PayMongoPaymentIntentId = paymentIntentId;
            payment.PayMongoChannel = channel;
            payment.PaymentMethod = MapChannelToMethod(channel);
            payment.Notes = "PayMongo online checkout (paid)";
            payment.ModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            // Update the parent SalesOrder's PaymentStatus
            if (payment.SalesOrderId.HasValue)
                await UpdateSalesOrderPaymentStatus(payment.SalesOrderId.Value);
        }

        return Ok();
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private async Task PopulateDropdowns(string? type)
    {
        var salesOrders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Where(so => so.PaymentStatus != "Paid" && so.Status != "Cancelled")
            .OrderByDescending(so => so.OrderDate)
            .Take(100)
            .ToListAsync();

        ViewBag.SalesOrders = new SelectList(
            salesOrders.Select(so => new
            {
                so.Id,
                Display = $"{so.SONumber} — {so.Customer?.CompanyName} (₱{(so.TotalAmount ?? 0):N2})"
            }),
            "Id", "Display");

        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Where(po => po.Status != "Cancelled")
            .OrderByDescending(po => po.OrderDate)
            .Take(100)
            .ToListAsync();

        ViewBag.PurchaseOrders = new SelectList(
            purchaseOrders.Select(po => new
            {
                po.Id,
                Display = $"{po.PONumber} — {po.Supplier?.CompanyName} (₱{(po.TotalAmount ?? 0):N2})"
            }),
            "Id", "Display");
    }

    private async Task<string> GenerateNextPaymentNumber()
    {
        var last = await _context.Payments
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        int next = (last?.Id ?? 0) + 1;
        return $"PAY-{DateTime.Now:yyyyMM}-{next:D4}";
    }

    private async Task UpdateSalesOrderPaymentStatus(int salesOrderId)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(salesOrderId);
        if (salesOrder == null) return;

        var totalPaid = await _context.Payments
            .Where(p => p.PaymentType == "Customer" && p.SalesOrderId == salesOrderId)
            .SumAsync(p => p.Amount);

        var total = salesOrder.TotalAmount ?? 0;

        if (total <= 0 || totalPaid <= 0)
            salesOrder.PaymentStatus = "Unpaid";
        else if (totalPaid >= total)
            salesOrder.PaymentStatus = "Paid";
        else
            salesOrder.PaymentStatus = "Partial";

        salesOrder.ModifiedDateTime = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Verifies the Paymongo-Signature header.
    /// Header format: t=timestamp,te=test_sig,li=live_sig
    /// Signature = HMAC-SHA256(timestamp + "." + body, webhookSecret)
    /// </summary>
    private static bool VerifyPayMongoSignature(string body, string signatureHeader, string secret)
    {
        try
        {
            var parts = signatureHeader
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            if (!parts.TryGetValue("t", out var timestamp))
                return false;

            // Use live signature if present, else test signature
            var signature = parts.TryGetValue("li", out var li) ? li
                          : parts.TryGetValue("te", out var te) ? te
                          : null;

            if (string.IsNullOrEmpty(signature))
                return false;

            var payload = $"{timestamp}.{body}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expected = Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature));
        }
        catch
        {
            return false;
        }
    }

    private static PaymentMethod MapChannelToMethod(string? channel) => channel?.ToLowerInvariant() switch
    {
        "gcash" => PaymentMethod.GCash,
        "paymaya" or "maya" => PaymentMethod.Maya,
        "card" => PaymentMethod.Card,
        "dob" => PaymentMethod.BankTransfer,
        "qrph" => PaymentMethod.QRPh,
        _ => PaymentMethod.Other
    };
}