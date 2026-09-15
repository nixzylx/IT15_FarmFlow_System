using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class CrmInteraction
{
    [Key]
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }

    [Required]
    [MaxLength(30)]
    public string InteractionType { get; set; } = string.Empty;  // Call, Email, Meeting, Complaint, Follow-up

    public DateTime InteractionDate { get; set; } = DateTime.Now;

    [MaxLength(150)]
    public string? Subject { get; set; }

    public string? Details { get; set; }

    public DateTime? NextFollowUpDate { get; set; }
    public bool IsResolved { get; set; } = false;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual Customer? Customer { get; set; }
    public virtual User? SalesRep { get; set; }
}