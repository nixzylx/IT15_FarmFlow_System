using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Quotation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string QuotationNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }

    public DateTime QuotationDate { get; set; } = DateTime.Now;
    public DateTime? ValidUntilDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    public decimal? SubtotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public string? Notes { get; set; }
    public int? ConvertedToSOId { get; set; }

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual User? SalesRep { get; set; }
}