using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmFlow.Web.Models;

public class SalesOrderItem
{
    [Key]
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public int InventoryItemId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityOrdered { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityDelivered { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLineAmount { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

    // Navigation
    public virtual SalesOrder? SalesOrder { get; set; }
    public virtual InventoryItem? InventoryItem { get; set; }
}