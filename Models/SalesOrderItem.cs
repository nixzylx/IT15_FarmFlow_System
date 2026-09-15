using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class SalesOrderItem
{
    [Key]
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public int InventoryItemId { get; set; }

    public decimal QuantityOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? QuantityPicked { get; set; }
    public decimal? QuantityDelivered { get; set; }
    public decimal? TotalLineAmount { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    // Navigation
    public virtual SalesOrder? SalesOrder { get; set; }
    public virtual InventoryItem? InventoryItem { get; set; }
}