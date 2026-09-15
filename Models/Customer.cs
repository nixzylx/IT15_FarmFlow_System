using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? CustomerType { get; set; }  // Wholesaler, Retailer, Exporter, Individual

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? TaxId { get; set; }

    public decimal? CreditLimit { get; set; }
    public decimal? CurrentBalance { get; set; } = 0;

    [MaxLength(30)]
    public string? PaymentTerms { get; set; }

    [MaxLength(20)]
    public string? DiscountTier { get; set; }

    public int? AssignedSalesRepId { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual User? AssignedSalesRep { get; set; }
    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public virtual ICollection<CrmInteraction> CrmInteractions { get; set; } = new List<CrmInteraction>();
    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}