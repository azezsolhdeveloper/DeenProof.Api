// DeenProof.Api/Entities/Claim.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeenProof.Api.Entities;

public class Claim
{
    public int Id { get; set; }

    [Required]
    public string ClaimAr { get; set; } = string.Empty;
    [Required]
    public string ClaimEn { get; set; } = string.Empty;

    [Required]
    public string ResponseAr { get; set; } = string.Empty;
    [Required]
    public string ResponseEn { get; set; } = string.Empty;

    public int Order { get; set; } // لترتيب الادعاءات داخل الرد

    // --- العلاقات ---
    [Required]
    public int DoubtId { get; set; }
    [ForeignKey("DoubtId")]
    public Doubt Doubt { get; set; } = null!;

    public ICollection<Source> Sources { get; set; } = new List<Source>();
}
