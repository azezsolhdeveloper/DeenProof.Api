using System.ComponentModel.DataAnnotations.Schema; // ✅ تأكد من وجود هذا السطر

namespace DeenProof.Api.Entities;
public class Feedback
{
    public int Id { get; set; }
    public int DoubtId { get; set; } // لربط الملاحظة بالشبهة
    public string Message { get; set; }
    public string? ContactInfo { get; set; } // اختياري (بريد إلكتروني أو غيره)
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    // --- ✅✅✅ بداية الإضافة المطلوبة ✅✅✅ ---
    [ForeignKey("DoubtId")]
    public Doubt Doubt { get; set; } = null!;
    // --- نهاية الإضافة ---
}
