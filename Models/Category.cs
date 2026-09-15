using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    // Navigation
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}