using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? TaxId { get; set; }

    [MaxLength(30)]
    public string? PaymentTerms { get; set; }  // Net 15, Net 30, COD, Prepaid

    public decimal? CreditLimit { get; set; }

    public decimal? PerformanceRating { get; set; }  // 1-5 rating

    [MaxLength(255)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}