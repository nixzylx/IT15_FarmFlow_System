using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Farm
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FarmName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? FarmCode { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? Province { get; set; }

    public decimal? TotalHectares { get; set; }

    [MaxLength(50)]
    public string? FarmType { get; set; }  // e.g., "Crop", "Livestock", "Mixed"

    [MaxLength(20)]
    public string? SoilType { get; set; }  // e.g., "Clay", "Sandy", "Loam"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation Properties (future use)
    // public virtual ICollection<ProductionBatch> ProductionBatches { get; set; }
}