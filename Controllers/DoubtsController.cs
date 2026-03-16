using DeenProof.Api.Data;
using DeenProof.Api.DTOs;
using DeenProof.Api.Entities;
using Microsoft.AspNetCore.Authorization; // ✅ 1. إضافة using للمصادقة
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims; // ✅ 2. إضافة using للوصول إلى بيانات التوكن
using System.Text.Json.Serialization; // ✅✅✅ أضف هذا السطر ✅✅✅

namespace DeenProof.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ✅ 3. حماية كل الـ Controller: لا يمكن لأي زائر الوصول إليه
    public class DoubtsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DoubtsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDoubts()
        {
            var doubts = await _context.Doubts
                .AsNoTracking()
                .Include(d => d.Author)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.TitleAr,
                    Status = d.Status.ToString(),
                    AuthorName = d.Author.Name,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(doubts);
        }
        // --- ✅✅✅ بداية الإصلاح الحقيقي والنهائي (العودة للخطة الأصلية) ✅✅✅ ---

        // GET: api/doubts/research
        // Endpoint مخصصة للباحثين فقط
        [HttpGet("research")]
        [Authorize(Roles = "Researcher")]
        public async Task<ActionResult<IEnumerable<object>>> GetDoubtsForResearch()
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var doubts = await _context.Doubts
                .AsNoTracking()
                // الباحث يرى فقط الشبهات التي كتبها هو وفي حالة المسودة
                .Where(d => d.AuthorId == currentUserId && d.Status == DoubtStatus.Draft)
                .Include(d => d.Author)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.TitleAr,
                    Status = d.Status.ToString(),
                    AuthorName = d.Author != null ? d.Author.Name : "مستخدم محذوف",
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(doubts);
        }

        // GET: api/doubts/review
        [HttpGet("review")]
        [Authorize(Roles = "Reviewer, Admin, SuperAdmin")]
        public async Task<ActionResult<IEnumerable<object>>> GetDoubtsForReview()
        {
            var query = _context.Doubts
                .AsNoTracking()
                .Where(d => d.Status == DoubtStatus.PendingReview || d.Status == DoubtStatus.PendingApproval);

            var doubts = await query
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.TitleAr,
                    d.TitleEn,
                    Status = d.Status.ToString(),
                    // ✅ آمن: تحقق من وجود المؤلف قبل الوصول إلى اسمه
                    AuthorName = d.Author != null ? d.Author.Name : "مستخدم محذوف",
                    d.UpdatedAt,

                    d.LockedByReviewerId,

                    // ✅✅✅ الحل الآمن والنهائي هنا ✅✅✅
                    // استخدمنا الشرط الثلاثي (Ternary Operator) للتحقق من وجود المراجع
                    // قبل محاولة الوصول إلى اسمه. إذا كان `d.LockedByReviewer` هو `null`،
                    // فإننا نرجع `null` كقيمة للاسم.
                    LockedByReviewerName = d.LockedByReviewer != null ? d.LockedByReviewer.Name : null
                })
                .ToListAsync();

            return Ok(doubts);
        }
        // --- نهاية الإصلاح الحقيقي والنهائي ---

        [HttpGet("{id}")]
        public async Task<ActionResult<DoubtDetailDto>> GetDoubtById(int id)
        {
            var doubt = await _context.Doubts
                .AsNoTracking()
                .Include(d => d.Author)
                .Include(d => d.DetailedRebuttal)
                    .ThenInclude(c => c.Sources)
                .Include(d => d.MainSources)
                .Include(d => d.Comments)       // 1. تضمين التعليقات
                    .ThenInclude(c => c.Author) // 2. ثم تضمين مؤلف كل تعليق
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doubt == null)
            {
                return NotFound();
            }

            var doubtDto = new DoubtDetailDto
            {
                Id = doubt.Id,
                TitleAr = doubt.TitleAr,
                TitleEn = doubt.TitleEn,
                SummaryAr = doubt.SummaryAr,
                SummaryEn = doubt.SummaryEn,
                QuickReplyAr = doubt.QuickReplyAr,
                QuickReplyEn = doubt.QuickReplyEn,
                Status = doubt.Status.ToString(),
                CreatedAt = doubt.CreatedAt,
                Category = doubt.Category,
                Slug = doubt.Slug,
                AuthorName = doubt.Author?.Name ?? "مستخدم محذوف",
                DetailedRebuttal = doubt.DetailedRebuttal.Select(c => new ClaimDto
                {
                    Id = c.Id,
                    ClaimAr = c.ClaimAr,
                    ClaimEn = c.ClaimEn,
                    ResponseAr = c.ResponseAr,
                    ResponseEn = c.ResponseEn,
                    Sources = c.Sources.Select(s => new SourceDto { Id = s.Id, NameAr = s.NameAr, NameEn = s.NameEn, Url = s.Url }).ToList()

                }).ToList(),
                MainSources = doubt.MainSources.Select(s => new SourceDto { Id = s.Id, NameAr = s.NameAr, NameEn = s.NameEn, Url = s.Url }).ToList(),


                // --- ✅✅✅ بداية الإصلاح الحقيقي والنهائي ✅✅✅ ---
                Comments = doubt.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    // 3. الآن، نستخدم العلاقة الصحيحة التي قمنا بتضمينها
                    AuthorName = c.Author?.Name ?? "مستخدم محذوف"
                }).ToList()
                // --- نهاية الإصلاح ---
            };

            return Ok(doubtDto);
        }

        [HttpPost]
        [Authorize(Roles = "Researcher, Admin, SuperAdmin")]
        public async Task<IActionResult> CreateDoubt([FromBody] CreateDoubtDto doubtDto)
        {
            var authorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(authorIdStr) || !int.TryParse(authorIdStr, out var authorId))
            {
                return Unauthorized("Invalid user token.");
            }

            var newDoubt = new Doubt
            {
                TitleAr = doubtDto.TitleAr,
                TitleEn = doubtDto.TitleEn,
                SummaryAr = doubtDto.SummaryAr,
                SummaryEn = doubtDto.SummaryEn,
                Category = doubtDto.Category,
                AuthorId = authorId,
                Status = DoubtStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                // ✅ استخدام المنطق الجديد
                Slug = GenerateSlug(doubtDto.TitleEn, doubtDto.TitleAr)
            };

            _context.Doubts.Add(newDoubt);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDoubtById), new { id = newDoubt.Id }, new { id = newDoubt.Id });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoubt(int id, [FromBody] UpdateDoubtDto doubtDto)
        {
          
            var doubt = await _context.Doubts.Include(d => d.MainSources).FirstOrDefaultAsync(d => d.Id == id);
            if (doubt == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            bool isOwner = doubt.AuthorId == currentUserId;
            bool isAdmin = currentUserRole == "Admin" || currentUserRole == "SuperAdmin";

            if (!isOwner && !isAdmin)
            {
                return Forbid("ليس لديك صلاحية تعديل هذا الرد.");
            }

            doubt.TitleAr = doubtDto.TitleAr;
            doubt.TitleEn = doubtDto.TitleEn;
            doubt.SummaryAr = doubtDto.SummaryAr;
            doubt.SummaryEn = doubtDto.SummaryEn;
            doubt.QuickReplyAr = doubtDto.QuickReplyAr;
            doubt.QuickReplyEn = doubtDto.QuickReplyEn;
            doubt.Category = doubtDto.Category;
            doubt.UpdatedAt = DateTime.UtcNow;
            // ✅ تحديث الـ Slug بذكاء: إذا أرسل المستخدم Slug مخصص نستخدمه، وإلا نولده من العناوين
            if (!string.IsNullOrWhiteSpace(doubtDto.Slug))
            {
                doubt.Slug = GenerateSlug(doubtDto.Slug, "");
            }
            else
            {
                doubt.Slug = GenerateSlug(doubtDto.TitleEn, doubtDto.TitleAr);
            }

            if (doubtDto.MainSources != null)
            {
                // ✅ 1. تحويل المصادر الحالية إلى قائمة لسهولة التعامل معها
                var existingSources = doubt.MainSources.ToList();

                // ✅ 2. حذف المصادر التي لم تعد موجودة في القائمة المرسلة (باستثناء الجديدة التي تحمل id=0)
                _context.Sources.RemoveRange(existingSources.Where(s => !doubtDto.MainSources.Any(dto => dto.Id == s.Id && dto.Id != 0)));

                // ✅ 3. تحديث المصادر الموجودة وإضافة الجديدة
                foreach (var sourceDto in doubtDto.MainSources)
                {
                    if (sourceDto.Id != 0)
                    {
                        // تحديث مصدر موجود
                        var existingSource = existingSources.FirstOrDefault(s => s.Id == sourceDto.Id);
                        if (existingSource != null)
                        {
                            existingSource.NameAr = sourceDto.NameAr; // ✅ تعديل هنا
                            existingSource.NameEn = sourceDto.NameEn; // ✅ تعديل هنا
                            existingSource.Url = sourceDto.Url;
                        }
                    }
                    else
                    {
                        // إضافة مصدر جديد
                        doubt.MainSources.Add(new Source
                        {
                            NameAr = sourceDto.NameAr, // ✅ تعديل هنا
                            NameEn = sourceDto.NameEn, // ✅ تعديل هنا
                            Url = sourceDto.Url
                        });
                    }
                }
            }



            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, SuperAdmin")] // ✅ 7. فقط المدير يمكنه الحذف
        public async Task<IActionResult> DeleteDoubt(int id)
        {
            var doubt = await _context.Doubts.FindAsync(id);
            if (doubt == null) return NotFound();

            _context.Doubts.Remove(doubt);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // POST: api/doubts/{doubtId}/claims
        [HttpPost("{doubtId}/claims")]
        public async Task<ActionResult<ClaimDto>> AddClaimToDoubt(int doubtId, [FromBody] ClaimDto claimDto)
        {
            var doubtExists = await _context.Doubts.AnyAsync(d => d.Id == doubtId);
            if (!doubtExists) return NotFound("Doubt not found.");

            var newClaim = new Entities.Claim
            {
                DoubtId = doubtId,
                ClaimAr = claimDto.ClaimAr,
                ClaimEn = claimDto.ClaimEn,
                ResponseAr = claimDto.ResponseAr,
                ResponseEn = claimDto.ResponseEn,
                // --- ✅✅✅ 1. بداية الإصلاح الحقيقي والنهائي ✅✅✅ ---
                // نقوم بتحويل كل SourceDto إلى كيان Source جديد
                Sources = claimDto.Sources.Select(s => new Source { NameAr = s.NameAr, NameEn = s.NameEn, Url = s.Url }).ToList()

            };

            _context.Claims.Add(newClaim);
            await _context.SaveChangesAsync();

            // --- ✅✅✅ 2. نرجع DTO كامل للادعاء الجديد مع مصادره وأرقامها التعريفية ---
            var resultDto = new ClaimDto
            {
                Id = newClaim.Id,
                ClaimAr = newClaim.ClaimAr,
                ClaimEn = newClaim.ClaimEn,
                ResponseAr = newClaim.ResponseAr,
                ResponseEn = newClaim.ResponseEn,
                Sources = newClaim.Sources.Select(s => new SourceDto { Id = s.Id, NameAr = s.NameAr, NameEn = s.NameEn, Url = s.Url }).ToList()

            };

            return Ok(resultDto);
            // --- نهاية الإصلاح ---
        }
        [HttpPut("claims/{claimId}")]
        public async Task<IActionResult> UpdateClaim(int claimId, [FromBody] ClaimDto claimDto)
        {
            // 1. ابحث عن الادعاء في قاعدة البيانات، وقم بتضمين مصادره
            var claim = await _context.Claims
                .Include(c => c.Sources)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null)
            {
                return NotFound(new { message = "Claim not found." });
            }

            // 2. قم بتحديث الخصائص الأساسية للادعاء
            claim.ClaimAr = claimDto.ClaimAr;
            claim.ClaimEn = claimDto.ClaimEn;
            claim.ResponseAr = claimDto.ResponseAr;
            claim.ResponseEn = claimDto.ResponseEn;

            // 3. قم بتحديث المصادر المرتبطة بهذا الادعاء (منطق معقد ومهم)
            if (claimDto.Sources != null)
            {
                // حذف المصادر القديمة التي لم تعد موجودة في الطلب الجديد
                var sourcesToDelete = claim.Sources
                    .Where(s => !claimDto.Sources.Any(dto => dto.Id == s.Id && dto.Id != 0))
                    .ToList();
                _context.Sources.RemoveRange(sourcesToDelete);

                // تحديث المصادر الموجودة وإضافة الجديدة
                foreach (var sourceDto in claimDto.Sources)
                {
                    var existingSource = claim.Sources.FirstOrDefault(s => s.Id == sourceDto.Id && s.Id != 0);
                    if (existingSource != null)
                    {
                        existingSource.NameAr = sourceDto.NameAr; // ✅ تعديل هنا
                        existingSource.NameEn = sourceDto.NameEn; // ✅ تعديل هنا
                        existingSource.Url = sourceDto.Url;
                    }
                    else
                    {
                        claim.Sources.Add(new Source
                        {
                            NameAr = sourceDto.NameAr, // ✅ تعديل هنا
                            NameEn = sourceDto.NameEn, // ✅ تعديل هنا
                            Url = sourceDto.Url
                        });
                    }
                }
            }

            // 4. احفظ كل التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();

            // 5. أرجع استجابة "لا يوجد محتوى" للإشارة إلى النجاح
            return NoContent();
        }
        // DELETE: api/doubts/claims/5  <-- المسار أبسط وأكثر منطقية
        [HttpDelete("claims/{claimId}")]
        public async Task<IActionResult> DeleteClaim(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
            {
                return NotFound("Claim not found.");
            }
            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{doubtId}/comments")]
        public async Task<ActionResult<object>> AddCommentToDoubt(int doubtId, [FromBody] AddCommentDto commentDto)
        {
            var doubtExists = await _context.Doubts.AnyAsync(d => d.Id == doubtId);
            if (!doubtExists)
            {
                return NotFound("Doubt not found.");
            }

            var authorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authorIdStr, out var authorId))
            {
                return Unauthorized("Invalid user token.");
            }

            var newComment = new Comment
            {
                Content = commentDto.Content,
                Section = commentDto.Section, // ✅ تم إضافة حقل القسم
                IsInternal = true,            // ✅ كل التعليقات الآن داخلية
                DoubtId = doubtId,
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            // ✅ نرجع كائنًا يتطابق تمامًا مع ما تتوقعه الواجهة الأمامية
            var result = new
            {
                newComment.Id,
                newComment.Content,
                newComment.Section,
                newComment.CreatedAt,
                AuthorName = User.FindFirstValue(ClaimTypes.Name) // نرسل اسم المستخدم الحالي مباشرة
            };

            return Ok(result);
        }

        public class UpdateStatusRequest
        {
            // نستخدم JsonPropertyName لنضمن التطابق بغض النظر عن إعدادات السيرفر
            [JsonPropertyName("newStatus")]
            [Required]
            public string NewStatus { get; set; }
        }

        [HttpPost("{id}/status")]
        [Authorize(Roles = "Researcher, Reviewer, Admin, SuperAdmin")]
        public async Task<IActionResult> UpdateDoubtStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. جلب الشبهة مع كل علاقاتها لمنع فقدان البيانات (الحل الأول)
            var doubt = await _context.Doubts
                .Include(d => d.DetailedRebuttal)
                .Include(d => d.MainSources)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doubt == null)
            {
                return NotFound(new { message = "Doubt not found." });
            }

            if (!Enum.TryParse<DoubtStatus>(request.NewStatus, true, out var newStatusEnum))
            {
                return BadRequest(new { message = $"Invalid status value: {request.NewStatus}" });
            }

            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(currentUserIdStr) || !int.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized("Invalid user token.");
            }
            bool isOwner = doubt.AuthorId == currentUserId;

            // منطق التحقق من الصلاحيات
            switch (newStatusEnum)
            {
                case DoubtStatus.PendingReview:
                    if (!isOwner)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "ليس لديك صلاحية إرسال هذا الرد للمراجعة." });
                    }
                    break;

                case DoubtStatus.NeedsRevision:
                    if (currentUserRole != "Reviewer" && currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "ليس لديك صلاحية طلب تعديل على هذا الرد." });
                    }
                    break;

                case DoubtStatus.Published:
                    if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "ليس لديك صلاحية نشر هذا الرد." });
                    }
                    break;
            }

            // 2. منطق تسجيل المراجع الصحيح (الحل الثاني)
            if (newStatusEnum == DoubtStatus.PendingApproval)
            {
                if (currentUserRole == "Reviewer" && doubt.ReviewerId == null)
                {
                    doubt.ReviewerId = currentUserId;
                }
            }

            // تحديث الحالة وتاريخ النشر
            doubt.Status = newStatusEnum;
            doubt.UpdatedAt = DateTime.UtcNow;
            if (newStatusEnum == DoubtStatus.Published && doubt.PublishedAt == null)
            {
                doubt.PublishedAt = DateTime.UtcNow;
            }

            // حفظ التغييرات
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Status updated successfully.",
                    newStatus = doubt.Status.ToString(),
                    publishedAt = doubt.PublishedAt
                });
            }
            catch (DbUpdateException ex)
            {
                // يمكنك تسجيل الخطأ هنا في نظام تسجيل حقيقي إذا أردت
                return StatusCode(500, new { message = "An error occurred while updating the database.", details = ex.Message });
            }
        }
        [HttpGet("my-library")]
        [Authorize(Roles = "Researcher")] // ✅ مخصصة للباحثين فقط
        public async Task<ActionResult<IEnumerable<object>>> GetMyLibrary()
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Unauthorized("Invalid user token.");
            }

            var doubts = await _context.Doubts
                .AsNoTracking()
                // 1. الشرط الوحيد هو أن يكون الباحث هو المؤلف
                .Where(d => d.AuthorId == currentUserId)
                .Include(d => d.Author)
                .OrderByDescending(d => d.UpdatedAt) // 2. نرتب حسب آخر تحديث
                .Select(d => new
                {
                    d.Id,
                    d.TitleAr,
                    d.TitleEn, // ✅ من الجيد إرسال العنوان الإنجليزي أيضًا
                    Status = d.Status.ToString(),
                    d.UpdatedAt
                })
                .ToListAsync();

            return Ok(doubts);
        }
        [HttpGet("search-all")]
        [Authorize(Roles = "Admin, SuperAdmin, Reviewer")]
public async Task<IActionResult> SearchAllDoubts(
    [FromQuery] string? searchTerm,
    [FromQuery] string? status, // فلتر حسب الحالة (e.g., "Published", "Draft")
    [FromQuery] int? authorId)   // فلتر حسب المؤلف
                                 // يمكنك إضافة المزيد من الفلاتر هنا في المستقبل (مثل التاريخ)
        {
            // نبدأ بالاستعلام الأساسي الذي يجلب كل شيء
            var query = _context.Doubts.Include(d => d.Author).AsQueryable();

            // تطبيق الفلاتر بشكل ديناميكي
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.TitleAr.Contains(searchTerm) || (d.TitleEn != null && d.TitleEn.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                // التحقق من أن الحالة المرسلة صالحة قبل استخدامها في الاستعلام
                if (Enum.TryParse<DoubtStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(d => d.Status == statusEnum);
                }
            }

            if (authorId.HasValue)
            {
                query = query.Where(d => d.AuthorId == authorId.Value);
            }

            // تنفيذ الاستعلام النهائي
            var doubts = await query
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.TitleAr,
                    d.TitleEn,
                    Status = d.Status.ToString(),
                    AuthorName = d.Author != null ? d.Author.Name : "مستخدم محذوف",
                    d.UpdatedAt
                })
                .ToListAsync();

            return Ok(doubts);
        }

        [HttpPost("{id}/lock")]
        [Authorize(Roles = "Reviewer, Admin, SuperAdmin")]
        public async Task<IActionResult> LockDoubtForReview([FromRoute] int id) // ✅✅✅ أضف [FromRoute] هنا
        {
            var doubt = await _context.Doubts.FindAsync(id);
            if (doubt == null)
            {
                return NotFound(new { message = "Doubt not found." });
            }

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (doubt.LockedByReviewerId != null && doubt.LockedByReviewerId != currentUserId && doubt.LockedAt.HasValue && doubt.LockedAt.Value.AddMinutes(60) > DateTime.UtcNow)
            {
                var lockedBy = await _context.Users.FindAsync(doubt.LockedByReviewerId);
                return StatusCode(StatusCodes.Status409Conflict, new { message = $"هذه المراجعة محجوزة حاليًا بواسطة {lockedBy?.Name ?? "مراجع آخر"}. يرجى المحاولة لاحقًا." });
            }

            doubt.LockedByReviewerId = currentUserId;
            doubt.LockedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حجز المراجعة لك بنجاح.", lockedAt = doubt.LockedAt, lockedBy = currentUserId });
        }

        // POST: api/doubts/{id}/unlock
        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "Reviewer, Admin, SuperAdmin")]
        public async Task<IActionResult> UnlockDoubt([FromRoute] int id) // ✅✅✅ أضف [FromRoute] هنا
        {
            var doubt = await _context.Doubts.FindAsync(id);
            if (doubt == null)
            {
                return NotFound(new { message = "Doubt not found." });
            }

            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (doubt.LockedByReviewerId == currentUserId)
            {
                doubt.LockedByReviewerId = null;
                doubt.LockedAt = null;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "تم إلغاء حجز المراجعة." });
        }
        private string GenerateSlug(string titleEn, string titleAr)
        {
            // 1. نحاول استخدام العنوان الإنجليزي أولاً لأنه الأفضل للروابط
            string source = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : titleAr;

            if (string.IsNullOrWhiteSpace(source))
                return Guid.NewGuid().ToString().Substring(0, 8);

            string slug = source.Trim().ToLower();

            // 2. تنظيف النص: استبدال المسافات والرموز بـ شرطة
            // هذه الركس تدعم الأحرف الإنجليزية والعربية معاً
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\u0600-\u06FF]+", "-");

            return slug.Trim('-');
        }
    }
}
