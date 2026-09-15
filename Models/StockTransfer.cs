using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class StockTransfer
{
    [Key]
    public int Id { get; set; }

    public int InventoryItemId { get; set; }
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }

    public decimal Quantity { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public int? PerformedByUserId { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.Now;
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    // Navigation
    public virtual InventoryItem? InventoryItem { get; set; }
    public virtual WarehouseLocation? FromLocation { get; set; }
    public virtual WarehouseLocation? ToLocation { get; set; }
    public virtual User? PerformedByUser { get; set; }
}