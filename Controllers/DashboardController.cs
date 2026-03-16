// DeenProof.Api/Controllers/DashboardController.cs
using DeenProof.Api.Data;
using DeenProof.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DeenProof.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary()
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized("Invalid user token.");
            }
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // --- 1. تخصيص الإحصائيات (يبقى كما هو) ---
            object stats;
            if (currentUserRole == "Researcher")
            {
                var myDrafts = await _context.Doubts.CountAsync(d => d.AuthorId == currentUserId && d.Status == DoubtStatus.Draft);
                var myRevisions = await _context.Doubts.CountAsync(d => d.AuthorId == currentUserId && d.Status == DoubtStatus.NeedsRevision);
                stats = new { myDrafts, myRevisions };
            }
            else if (currentUserRole == "Reviewer")
            {
                var pendingReview = await _context.Doubts.CountAsync(d => d.Status == DoubtStatus.PendingReview || d.Status == DoubtStatus.PendingApproval);
                var needsRevision = await _context.Doubts.CountAsync(d => d.Status == DoubtStatus.NeedsRevision);
                stats = new { pendingReview, needsRevision };
            }
            else // Admin & SuperAdmin
            {
                var totalDoubts = await _context.Doubts.CountAsync();
                var pendingReview = await _context.Doubts.CountAsync(d => d.Status == DoubtStatus.PendingReview || d.Status == DoubtStatus.PendingApproval);
                var published = await _context.Doubts.CountAsync(d => d.Status == DoubtStatus.Published);
                var needsRevision = await _context.Doubts.CountAsync(d => d.Status == DoubtStatus.NeedsRevision);
                var totalLikes = await _context.Doubts.SumAsync(d => d.LikeCount);

                stats = new { totalDoubts, pendingReview, published, needsRevision, totalLikes }; // ✅ أضف totalLikes هنا
            }

            // --- ✅✅✅ بداية الإصلاح الحقيقي والنهائي (منطق المهام) ✅✅✅ ---
            var myTasksQuery = _context.Doubts.AsQueryable();

            if (currentUserRole == "Researcher")
            {
                // الباحث يرى فقط الشبهات التي أُعيدت إليه للتعديل
                myTasksQuery = myTasksQuery.Where(d => d.AuthorId == currentUserId && d.Status == DoubtStatus.NeedsRevision);
            }
            else if (currentUserRole == "Reviewer")
            {
                // المراجع يرى الشبهات التي تنتظر المراجعة أو الموافقة (والتي لم يكتبها هو)
                myTasksQuery = myTasksQuery.Where(d =>
        d.Status == DoubtStatus.PendingReview
        && d.AuthorId != currentUserId
    );

                // **ثم**، طبق فلتر القفل على هذه النتائج
                myTasksQuery = myTasksQuery.Where(d =>
                    // أرني المهمة فقط إذا كانت...
                    // (أ) غير مقفولة على الإطلاق
                    d.LockedByReviewerId == null ||
                    // (ب) أو مقفولة من قبلي أنا شخصيًا
                    d.LockedByReviewerId == currentUserId ||
                    // (ج) أو أن القفل قديم جدًا (انتهت صلاحيته)
                    (d.LockedAt.HasValue && d.LockedAt.Value.AddMinutes(60) < DateTime.UtcNow)
                );
            }
            else if (currentUserRole == "Admin" || currentUserRole == "SuperAdmin")
            {
                // ✅ المدير يرى كل المهام التي تنتظر إجراء
                myTasksQuery = myTasksQuery.Where(d => d.Status == DoubtStatus.PendingReview || d.Status == DoubtStatus.PendingApproval || d.Status == DoubtStatus.NeedsRevision);
            }
            else
            {
                // أي دور آخر لا يرى مهام
                myTasksQuery = myTasksQuery.Where(d => false);
            }

            var myTasks = await myTasksQuery
       .Include(d => d.Author)
       .OrderByDescending(d => d.UpdatedAt)
       .Select(d => new {
           id = d.Id,
           titleAr = d.TitleAr,
           titleEn = d.TitleEn,
           authorName = d.Author.Name,
           updatedAt = d.UpdatedAt,
           status = d.Status.ToString() // ✅✅✅ أضف هذا السطر المهم ✅✅✅
       }).ToListAsync();

            // --- نهاية الإصلاح ---

            // --- 3. تخصيص آخر النشاطات (يبقى كما هو) ---
            var activitiesQuery = _context.Doubts
                .Include(d => d.Author)
                .AsQueryable();

            if (currentUserRole == "Researcher")
            {
                activitiesQuery = activitiesQuery.Where(d => d.AuthorId == currentUserId);
            }
            else if (currentUserRole == "Reviewer")
            {
                activitiesQuery = activitiesQuery.Where(d => d.Status == DoubtStatus.PendingReview || d.Status == DoubtStatus.NeedsRevision);
            }

            var recentActivities = await activitiesQuery
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => new {
                    id = d.Id,
                    userName = d.Author.Name,
                    actionKey = "addedNewDoubt",
                    doubtTitleAr = d.TitleAr,
                    doubtTitleEn = d.TitleEn,
                    timestamp = d.CreatedAt
                }).ToListAsync();

            var dashboardData = new { stats, myTasks, recentActivities };
            return Ok(dashboardData);
        }

        // GET: api/dashboard/feedback
        [HttpGet("feedback")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<ActionResult<IEnumerable<object>>> GetFeedbackSubmissions()
        {
            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.Doubt)
                .OrderByDescending(f => f.SubmittedAt)
                .Select(f => new
                {
                    f.Id,
                    f.Message,
                    f.ContactInfo,
                    f.IsRead,
                    f.SubmittedAt,
                    DoubtId = f.Doubt.Id,
                    DoubtTitle = f.Doubt.TitleAr,
                    DoubtSlug = f.Doubt.Slug,
                    DoubtCategory = f.Doubt.Category // ✅✅✅ أضف هذا السطر المهم ✅✅✅
                })
                .ToListAsync();

            return Ok(feedbacks);
        }


        [HttpPost("feedback/{id}/status")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> UpdateFeedbackStatus(int id, [FromBody] UpdateFeedbackStatusDto model)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound(new { message = "Feedback not found." });
            }

            feedback.IsRead = model.IsRead;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Feedback status updated successfully." });
        }

        // لا تنسى إنشاء DTO لاستقبال البيانات
        public class UpdateFeedbackStatusDto
        {
            [Required]
            public bool IsRead { get; set; }
        }
    }
}
