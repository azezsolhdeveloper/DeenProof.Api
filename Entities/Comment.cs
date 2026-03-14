// DeenProof.Api/Entities/Comment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeenProof.Api.Entities
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- ✅✅✅ بداية الإضافات الجديدة التي كانت مفقودة ✅✅✅ ---

        // هل هو تعليق داخلي (للفريق) أم عام (سيظهر للزوار)؟
        // القيمة الافتراضية هي 'true' لأن كل التعليقات الآن داخلية.
        public bool IsInternal { get; set; } = true;

        // القسم المتعلق به التعليق (e.g., "summary", "rebuttal")
        // علامة الاستفهام '?' تعني أن هذا الحقل اختياري ويمكن أن يكون null.
        public string? Section { get; set; }

        // --- نهاية الإضافات الجديدة ---

        // --- العلاقات ---
        [Required]
        public int AuthorId { get; set; } // من هو المستخدم الذي كتب التعليق

        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;

        [Required]
        public int DoubtId { get; set; } // التعليق مرتبط بأي رد

        [ForeignKey("DoubtId")]
        public Doubt Doubt { get; set; } = null!;
    }
}
