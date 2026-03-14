// DeenProof.Api/DTOs/DoubtDetailDto.cs
namespace DeenProof.Api.DTOs;

public class SourceDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Url { get; set; }
}

public class ClaimDto
{
    public int Id { get; set; }
    public string ClaimAr { get; set; } = string.Empty;
    public string ClaimEn { get; set; } = string.Empty;
    public string ResponseAr { get; set; } = string.Empty;
    public string ResponseEn { get; set; } = string.Empty;
    public List<SourceDto> Sources { get; set; } = new List<SourceDto>();
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
}

// DeenProof.Api/DTOs/DoubtDetailDto.cs

public class DoubtDetailDto
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string? QuickReplyAr { get; set; }
    public string? QuickReplyEn { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    // --- ✅✅✅ أضف هذين السطرين المفقودين هنا ✅✅✅ ---
    public string Category { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public List<ClaimDto> DetailedRebuttal { get; set; } = new();
    public List<SourceDto> MainSources { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
}

