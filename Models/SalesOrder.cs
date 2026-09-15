using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class SalesOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string SONumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? RequiredDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Confirmed";

    public decimal? SubtotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Unpaid";

    public string? DeliveryInstructions { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual User? SalesRep { get; set; }
    public virtual ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}