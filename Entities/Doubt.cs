// DeenProof.Api/Entities/Doubt.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeenProof.Api.Entities;

public enum DoubtStatus
{
    Draft,
    PendingReview,
    NeedsRevision,
    PendingApproval,
    Published,
    Archived
}

public class Doubt
{
    public int Id { get; set; }

    [Required, MaxLength(250)]
    public string TitleAr { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string TitleEn { get; set; } = string.Empty;

    // ✅✅✅ بداية الإضافات ✅✅✅
    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty; // مثل: "quran", "history"

    [Required, MaxLength(300)]
    public string Slug { get; set; } = string.Empty; // لعنوان URL: "why-was-quran-revealed-in-stages"
    // ✅✅✅ نهاية الإضافات ✅✅✅

    [Required]
    public string SummaryAr { get; set; } = string.Empty;

    [Required]
    public string SummaryEn { get; set; } = string.Empty;

    public string? QuickReplyAr { get; set; }
    public string? QuickReplyEn { get; set; }
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int? ReviewerId { get; set; } // مفتاح خارجي للمراجع (اختياري)
    public User? Reviewer { get; set; } // علاقة التنقل للمراجع
    public DoubtStatus Status { get; set; } = DoubtStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    // --- العلاقات (Foreign Keys) ---
    [Required]
    public int AuthorId { get; set; }
    [ForeignKey("AuthorId")]
    public User Author { get; set; } = null!;

    // 1. من هو المراجع الذي حجز هذه الشبهة؟
    public int? LockedByReviewerId { get; set; }
    public User? LockedByReviewer { get; set; }

    // 2. متى تم حجزها؟
    public DateTime? LockedAt { get; set; }
    // --- علاقات التنقل (Navigation Properties) ---
    public ICollection<Claim> DetailedRebuttal { get; set; } = new List<Claim>();
    public ICollection<Source> MainSources { get; set; } = new List<Source>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
