// DeenProof.Api/Entities/Source.cs
using System.ComponentModel.DataAnnotations;

namespace DeenProof.Api.Entities;

public class Source
{
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? NameEn { get; set; }
    public string? Url { get; set; }

    // --- العلاقات ---
    // هذا المصدر يمكن أن يكون مرتبطًا بالعديد من الردود والادعاءات
    public ICollection<Doubt> Doubts { get; set; } = new List<Doubt>();
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
