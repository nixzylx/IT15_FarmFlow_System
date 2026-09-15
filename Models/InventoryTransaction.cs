using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class InventoryTransaction
{
    [Key]
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;  // Receiving, Issue, Wastage, Transfer

    [MaxLength(30)]
    public string? ReferenceDocument { get; set; }  // PO number, Sales Order

    public decimal QuantityChange { get; set; }  // Positive = in, Negative = out
    public decimal? StockBefore { get; set; }
    public decimal? StockAfter { get; set; }
    public decimal? UnitCost { get; set; }

    [MaxLength(50)]
    public string? WarehouseLocation { get; set; }

    [MaxLength(255)]
    public string? Remarks { get; set; }

    public int? PerformedByUserId { get; set; }

    public DateTime TransactionDateTime { get; set; } = DateTime.Now;

    // Navigation
    public virtual InventoryItem? InventoryItem { get; set; }
    public virtual User? PerformedByUser { get; set; }
}