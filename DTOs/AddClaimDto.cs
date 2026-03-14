// DeenProof.Api/DTOs/AddClaimDto.cs
using System.ComponentModel.DataAnnotations;

namespace DeenProof.Api.DTOs;

public class AddClaimDto
{
    [Required]
    public string ClaimAr { get; set; } = string.Empty;
    [Required]
    public string ClaimEn { get; set; } = string.Empty;
    [Required]
    public string ResponseAr { get; set; } = string.Empty;
    [Required]
    public string ResponseEn { get; set; } = string.Empty;
    public List<int>? SourceIds { get; set; } // قائمة بـ IDs المصادر الموجودة مسبقًا
}
