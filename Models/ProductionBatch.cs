using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class ProductionBatch
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string BatchCode { get; set; } = string.Empty;

    public int FarmId { get; set; }
    public int CropId { get; set; }

    [MaxLength(50)]
    public string? FieldName { get; set; }

    public decimal? FieldAreaHectares { get; set; }

    public DateTime? PlantingDate { get; set; }
    public DateTime? ExpectedHarvestDate { get; set; }
    public DateTime? ActualHarvestDate { get; set; }

    [MaxLength(30)]
    public string? CurrentStage { get; set; }  // Planning, Planted, Growing, Harvesting, Completed

    [MaxLength(30)]
    public string? CropHealthStatus { get; set; }  // Excellent, Good, Fair, Poor, Diseased

    public decimal? PredictedYieldKg { get; set; }
    public decimal? ActualYieldKg { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation Properties
    public virtual Farm? Farm { get; set; }
    public virtual Crop? Crop { get; set; }
}