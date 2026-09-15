using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class PurchaseOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string PONumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public int? RequestedByUserId { get; set; }
    public int? ApprovedByUserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? DeliveryExpectedDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Draft";  // Draft, Pending Approval, Sent, Shipped, Received, Cancelled

    public decimal? SubtotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    [MaxLength(30)]
    public string? PaymentTerms { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual Supplier? Supplier { get; set; }
    public virtual User? RequestedByUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}