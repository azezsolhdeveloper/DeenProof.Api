// DeenProof.Api/DTOs/CreateDoubtDto.cs

using System.ComponentModel.DataAnnotations;

namespace DeenProof.Api.DTOs
{
    public class CreateDoubtDto
    {
        [Required, MaxLength(250)]
        public string TitleAr { get; set; } = string.Empty;

        [MaxLength(250)] // الإنجليزية اختيارية في البداية
        public string TitleEn { get; set; } = string.Empty;

        public string SummaryAr { get; set; } = string.Empty;
        public string SummaryEn { get; set; } = string.Empty;

        // --- ✅✅✅ بداية الإصلاح ✅✅✅ ---
        [Required]
        public string Category { get; set; } = string.Empty;
        // --- نهاية الإصلاح ---
    }
}
