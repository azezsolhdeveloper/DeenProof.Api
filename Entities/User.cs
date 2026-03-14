using System.ComponentModel.DataAnnotations;
namespace DeenProof.Api.Entities
{
    public enum UserRole
    {
        Researcher,     // 0
        Reviewer,       // 1
        Admin,          // 2
        SuperAdmin      // 3 (✅ إضافة جديدة)
    }
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
