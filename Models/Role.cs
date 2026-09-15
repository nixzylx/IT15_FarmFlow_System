using System.ComponentModel.DataAnnotations;

namespace FarmFlow.Web.Models;

public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public DateTime CreatedDateTime { get; set; }  // ← No default value!

    // Navigation Property
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}