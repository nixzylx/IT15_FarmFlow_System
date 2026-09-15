using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Crop
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string CropName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Variety { get; set; }

    [MaxLength(20)]
    public string? CropCode { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }  // Vegetable, Fruit, Grain, etc.

    public int? GrowthDurationDays { get; set; }

    public decimal? ExpectedYieldPerHectare { get; set; }

    [MaxLength(10)]
    public string? UnitOfMeasure { get; set; }  // kg, tons, pieces

    [MaxLength(50)]
    public string? Season { get; set; }  // Wet, Dry, Year-round

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation Properties
    public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
}