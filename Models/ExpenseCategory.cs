using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmFlow.Web.Models;

public class ExpenseCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}