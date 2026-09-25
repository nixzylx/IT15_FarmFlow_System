using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmFlow.Web.Models;

public class Expense
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string ExpenseNumber { get; set; } = string.Empty;   // EXP-202509-0001

    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    public int ExpenseCategoryId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(150)]
    public string? Vendor { get; set; }                         // Who was paid

    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }                // OR#, Invoice#

    // Optional links
    public int? FarmId { get; set; }
    public int? ProductionBatchId { get; set; }

    // Approval
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";             // Pending, Approved, Rejected, Paid

    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    [MaxLength(300)]
    public string? ApprovalNotes { get; set; }

    // Audit
    public int CreatedByUserId { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual ExpenseCategory? ExpenseCategory { get; set; }
    public virtual Farm? Farm { get; set; }
    public virtual ProductionBatch? ProductionBatch { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual User? CreatedByUser { get; set; }
}