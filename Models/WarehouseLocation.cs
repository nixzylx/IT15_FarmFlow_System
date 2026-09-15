using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class WarehouseLocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string LocationName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? LocationType { get; set; }  // Cold Storage, Dry Storage, Grain Silo

    public decimal? Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
}