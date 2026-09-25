using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmFlow.Web.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string PaymentNumber { get; set; } = string.Empty;   // PAY-202509-0001

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    public string PaymentType { get; set; } = "Customer";       // Customer | Supplier

    // Links (one will be populated based on PaymentType)
    public int? SalesOrderId { get; set; }
    public int? PurchaseOrderId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }                // Check #, GCash ref

    [MaxLength(500)]
    public string? Notes { get; set; }

    // ✅ NEW: PayMongo online payment tracking
    [MaxLength(100)]
    public string? PayMongoCheckoutSessionId { get; set; }

    [MaxLength(100)]
    public string? PayMongoPaymentIntentId { get; set; }

    [MaxLength(30)]
    public string? PayMongoChannel { get; set; }                // gcash, maya, card, dob

    // Audit
    public int CreatedByUserId { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual SalesOrder? SalesOrder { get; set; }
    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual User? CreatedByUser { get; set; }
}