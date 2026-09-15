using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ItemType { get; set; }  // Raw Material, Input, Finished Produce, Equipment

    public int? CategoryId { get; set; }

    [Required]
    [MaxLength(10)]
    public string UnitOfMeasure { get; set; } = "kg";  // kg, bags, liters, pieces

    public decimal? ReorderLevel { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal CurrentStock { get; set; } = 0;
    public decimal? CostPerUnit { get; set; }
    public decimal? SellingPricePerUnit { get; set; }

    [MaxLength(50)]
    public string? StorageLocation { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation
    public virtual Category? Category { get; set; }
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public virtual ICollection<StockTransfer> StockTransfers { get; set; } = new List<StockTransfer>();
}