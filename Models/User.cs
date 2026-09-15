using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    // Make PasswordHash NOT required for model validation
    // We'll validate password separately
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? MobileNumber { get; set; }

    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation Property
    public virtual Role? Role { get; set; }
}