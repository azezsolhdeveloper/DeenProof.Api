// DeenProof.Api/Controllers/PublicController.cs

using DeenProof.Api.Data;
using DeenProof.Api.Entities; // تأكد من وجود هذا السطر
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeenProof.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PublicController(ApplicationDbContext context)
    {
        _context = context;
    }

    // DeenProof.Api/Controllers/PublicController.cs

    [HttpGet("doubts")]
    // --- ✅ 1. إزالة متغير `lang` تمامًا. لم نعد بحاجة إليه هنا. ---
    public async Task<ActionResult<IEnumerable<object>>> GetPublishedDoubts([FromQuery] string? category)
    {
        var query = _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(d => d.Category == category);
        }

        var doubts = await query
            .OrderByDescending(d => d.PublishedAt)
            .Select(d => new
            {
                // --- ✅✅✅ 2. بداية الإصلاح الحقيقي والنهائي (نسخ المنطق الناجح) ✅✅✅ ---
                // الآن نرسل دائمًا البيانات الكاملة باللغتين، تمامًا مثل دالة التفاصيل
                d.Id,
                d.Slug,
                d.Category,
                d.TitleAr,
                d.TitleEn,
                d.SummaryAr,
                d.SummaryEn,
                d.PublishedAt
                // --- نهاية الإصلاح ---
            })
            .Take(20)
            .ToListAsync();

        return Ok(doubts);
    }

    [HttpGet("doubts/paginated")] // ‼️ اسم مسار جديد ومختلف ‼️
    public async Task<ActionResult<object>> GetPaginatedPublishedDoubts(
    [FromQuery] string? category,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var query = _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(d => d.Category == category);
        }

        var totalItems = await query.CountAsync();

        var doubts = await query
            .OrderByDescending(d => d.PublishedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.Slug,
                d.Category,
                d.TitleAr,
                d.TitleEn,
                d.SummaryAr,
                d.SummaryEn,
                d.PublishedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = doubts,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }
    [HttpGet("doubts/for-static-generation")]
    public async Task<ActionResult<IEnumerable<object>>> GetParamsForStaticGeneration()
    {
        var paramsList = await _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published)
            .Select(d => new
            {
                // نرسل فقط البيانات المطلوبة، لا أكثر
                d.Slug,
                d.Category
            })
            .Distinct() // نضمن عدم وجود تكرار
            .ToListAsync();

        return Ok(paramsList);
    }
    [HttpGet("doubts/all")]
    public async Task<ActionResult<IEnumerable<object>>> GetAllPublishedDoubts()
    {
        var doubts = await _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published)
            .OrderByDescending(d => d.PublishedAt)
            .Select(d => new
            {
                d.Id,
                d.Slug,
                d.Category,
                d.TitleAr,
                d.TitleEn,
                d.SummaryAr,
                d.SummaryEn,
                d.PublishedAt
            })
            .ToListAsync();

        return Ok(doubts);
    }
    // DeenProof.Api/Controllers/PublicController.cs

    [HttpGet("doubts/{slug}")]
    public async Task<ActionResult<object>> GetPublishedDoubtBySlug(string slug)
    {
        var decodedSlug = System.Net.WebUtility.UrlDecode(slug);

        // --- ✅ 1. جلب كل البيانات المطلوبة، بما في ذلك المراجع ---
        var doubt = await _context.Doubts
            .AsTracking()
            .Include(d => d.Author)
            .Include(d => d.Reviewer) // ✅✅✅ هذا هو السطر الجديد الذي يضيف المراجع
            .Include(d => d.DetailedRebuttal)
                .ThenInclude(c => c.Sources)
            .Include(d => d.MainSources)
            .FirstOrDefaultAsync(d => d.Slug == decodedSlug && d.Status == DoubtStatus.Published);

        if (doubt == null)
        {
            return NotFound();
        }

        // --- ✅ 2. زيادة عدد المشاهدات وحفظ التغيير ---
        doubt.ViewCount++;
        await _context.SaveChangesAsync();

        // --- ✅ 3. تحديد البيانات التي سيتم إرجاعها، بما في ذلك اسم المراجع ---
        var result = new
        {
            doubt.Id,
            doubt.Slug,
            doubt.Category,
            AuthorName = doubt.Author.Name,
            ReviewerName = doubt.Reviewer?.Name, // ✅✅✅ هذا هو السطر الجديد الذي يرسل اسم المراجع
            doubt.PublishedAt,
            doubt.TitleAr,
            doubt.TitleEn,
            doubt.SummaryAr,
            doubt.SummaryEn,
            doubt.QuickReplyAr,
            doubt.QuickReplyEn,
            DetailedRebuttal = doubt.DetailedRebuttal.Select(c => new
            {
                c.Id,
                c.ClaimAr,
                c.ClaimEn,
                c.ResponseAr,
                c.ResponseEn,
                Sources = c.Sources.Select(s => new { s.Id, s.NameAr, s.NameEn, s.Url }) // ✅ إصلاح هنا
            }),
            MainSources = doubt.MainSources.Select(s => new { s.Id, s.NameAr, s.NameEn, s.Url }),
            doubt.ViewCount,
            doubt.LikeCount
        };

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchDoubts([FromQuery] string query, [FromQuery] string lang = "ar")
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            // لا تبحث عن مصطلحات قصيرة جدًا
            return Ok(Enumerable.Empty<object>());
        }

        var normalizedQuery = query.ToLower().Trim();

        var results = await _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published)
            .Where(d =>
                (d.TitleAr.ToLower().Contains(normalizedQuery) || d.SummaryAr.ToLower().Contains(normalizedQuery)) ||
                (d.TitleEn.ToLower().Contains(normalizedQuery) || d.SummaryEn.ToLower().Contains(normalizedQuery))
            )
            .Select(d => new {
                // نختار البيانات التي تحتاجها الواجهة الأمامية فقط
                slug = d.Slug,
                category = d.Category,
                // نرسل العنوان والملخص بناءً على اللغة المطلوبة
                title = lang == "ar" ? d.TitleAr : d.TitleEn,
                summary = lang == "ar" ? d.SummaryAr : d.SummaryEn
            })
            .Take(5) // نحدد عدد النتائج بـ 5 لتكون القائمة سريعة ومختصرة
            .ToListAsync();

        return Ok(results);
    }
    [HttpGet("category-stats")]
    public async Task<ActionResult<IEnumerable<object>>> GetCategoryStats()
    {
        // ✅ الخطوة 1: جلب قائمة التصنيفات المنشورة فقط
        var publishedCategories = await _context.Doubts
            .Where(d => d.Status == DoubtStatus.Published)
            .Select(d => d.Category)
            .ToListAsync();

        // ✅ الخطوة 2: القيام بعملية التجميع في الذاكرة (in-memory)
        var stats = publishedCategories
            .GroupBy(category => category)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count()
            })
            .ToList();

        return Ok(stats);
    }
    [HttpGet("doubts/for-sitemap")]
public async Task<ActionResult<IEnumerable<object>>> GetDoubtsForSitemap()
    {
        var doubts = await _context.Doubts
            .AsNoTracking()
            .Where(d => d.Status == DoubtStatus.Published)
            .OrderByDescending(d => d.PublishedAt)
            .Select(d => new
            {
                // نختار فقط البيانات التي تحتاجها خريطة الموقع
                d.Slug,
                d.Category,
                d.PublishedAt
            })
            .ToListAsync();

        return Ok(doubts);
    }
    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmissionModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var feedback = new Feedback
        {
            DoubtId = model.DoubtId,
            Message = model.Message,
            ContactInfo = model.ContactInfo
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Feedback submitted successfully." });
    }

   


    [HttpPost("doubts/{id}/like")]
    public async Task<IActionResult> IncrementLikeCount(int id)
    {
        var doubt = await _context.Doubts.FindAsync(id);
        if (doubt == null)
        {
            return NotFound();
        }

        doubt.LikeCount++;
        await _context.SaveChangesAsync();

        return Ok(new { newLikeCount = doubt.LikeCount });
    }

    // نحتاج إلى تعريف النموذج الذي ستستقبله الدالة
    public class FeedbackSubmissionModel
    {
        public int DoubtId { get; set; }
        public string Message { get; set; }
        public string? ContactInfo { get; set; }
    }
}
