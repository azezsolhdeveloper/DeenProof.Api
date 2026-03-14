// DeenProof.Api/DTOs/UpdateDoubtDto.cs
namespace DeenProof.Api.DTOs
{
    public class UpdateDoubtDto
    {
        public string TitleAr { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string SummaryAr { get; set; } = string.Empty;
        public string SummaryEn { get; set; } = string.Empty;
        public string? QuickReplyAr { get; set; }
        public string? QuickReplyEn { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public List<SourceDto> MainSources { get; set; } = new List<SourceDto>();
        // لا يوجد حقل للحالة هنا، وهذا هو الصحيح.
    }
}
