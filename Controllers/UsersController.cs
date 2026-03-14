using DeenProof.Api.Data;
using DeenProof.Api.DTOs;
using DeenProof.Api.Entities;
using DeenProof.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using SecurityClaim = System.Security.Claims.Claim;

namespace DeenProof.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // كل الدوال محمية بشكل افتراضي
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly OwnerSettings _ownerSettings;

        public UsersController(ApplicationDbContext context, IConfiguration configuration, OwnerSettings ownerSettings)
        {
            _context = context;
            _configuration = configuration;
            _ownerSettings = ownerSettings;
        }

        // DeenProof.Api/Controllers/UsersController.cs

        [HttpGet]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var isCurrentUserOwner = User.HasClaim("is_owner", "true");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var query = _context.Users.AsQueryable();

            // --- ✅✅✅ بداية الإصلاح الحقيقي والنهائي ✅✅✅ ---

            if (isCurrentUserOwner)
            {
                // المالك يرى كل المستخدمين الآخرين (كل الأدوار).
                // لا نحتاج لفلترة إضافية هنا، فالمالك هو الجذر.
            }
            else if (currentUserRole == "SuperAdmin")
            {
                // المشرف العام يرى فقط الأدوار التي تحته (المدراء، المراجعون، الباحثون).
                // هو لا يرى المشرفين العامين الآخرين.
                query = query.Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Reviewer || u.Role == UserRole.Researcher);
            }
            else if (currentUserRole == "Admin")
            {
                // المدير يرى فقط الأدوار التي تحته (المراجعون والباحثون).
                query = query.Where(u => u.Role == UserRole.Reviewer || u.Role == UserRole.Researcher);
            }
            else
            {
                // في حالة وجود أي دور آخر وصل إلى هنا بطريق الخطأ، نرجع قائمة فارغة.
                query = query.Where(u => false);
            }

            // --- نهاية الإصلاح ---

            var users = await query.OrderBy(u => u.Name).ToListAsync();
            return Ok(users);
        }


        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginPayload payload)
        {
            if (string.IsNullOrEmpty(payload.Email) || string.IsNullOrEmpty(payload.Password))
            {
                return BadRequest("البريد الإلكتروني وكلمة المرور مطلوبان.");
            }

            if (payload.Email.Equals(_ownerSettings.Email, StringComparison.OrdinalIgnoreCase) && payload.Password == _ownerSettings.Password)
            {
                var token = GenerateOwnerJwtToken();
                return Ok(new { Token = token });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(payload.Password, user.PasswordHash))
            {
                return Unauthorized("البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            }

            var userToken = GenerateUserJwtToken(user);
            return Ok(new { Token = userToken });
        }

        [HttpPost]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<ActionResult<User>> CreateUser(CreateUserPayload payload)
        {
            var isCurrentUserOwner = User.HasClaim("is_owner", "true");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // --- ✅ 2. إصلاح منطق الإنشاء ---
            if (payload.Role == UserRole.SuperAdmin && !isCurrentUserOwner)
            {
                return Forbid("فقط المالك يمكنه إنشاء مشرف عام جديد.");
            }

            if (currentUserRole == "Admin" && (payload.Role == UserRole.Admin || payload.Role == UserRole.SuperAdmin))
            {
                return Forbid("المدير يمكنه فقط إنشاء باحثين ومراجعين.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == payload.Email))
            {
                return BadRequest("This email is already in use.");
            }

            var userToCreate = new User
            {
                Name = payload.Name,
                Email = payload.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password),
                Role = payload.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(userToCreate);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUserById), new { id = userToCreate.Id }, userToCreate);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto payload)
        {
            var isCurrentUserOwner = User.HasClaim("is_owner", "true");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // --- ✅ 3. إصلاح منطق التعديل ---
            if (userToUpdate.Role == UserRole.SuperAdmin && !isCurrentUserOwner)
            {
                return Forbid("فقط المالك يمكنه تعديل بيانات مشرف عام.");
            }

            if (currentUserRole == "Admin" && (userToUpdate.Role == UserRole.Admin || userToUpdate.Role == UserRole.SuperAdmin))
            {
                return Forbid("المدير لا يمكنه تعديل بيانات مدير آخر أو مشرف عام.");
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == payload.Email && u.Id != id);
            if (emailExists)
            {
                return BadRequest("This email is already in use by another user.");
            }

            userToUpdate.Name = payload.Name;
            userToUpdate.Email = payload.Email;
            userToUpdate.Role = payload.Role;

            if (!string.IsNullOrEmpty(payload.Password))
            {
                userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, SuperAdmin")] // نوسع الصلاحية هنا، لكن المنطق بالداخل هو الذي يحكم
        public async Task<IActionResult> DeleteUser(int id)
        {
            var isCurrentUserOwner = User.HasClaim("is_owner", "true");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null) return NotFound();

            // --- ✅ 4. إصلاح منطق الحذف ---
            if (userToDelete.Role == UserRole.SuperAdmin && !isCurrentUserOwner)
            {
                return Forbid("فقط المالك يمكنه حذف مشرف عام آخر.");
            }

            if (currentUserRole == "Admin" && (userToDelete.Role == UserRole.Admin || userToDelete.Role == UserRole.SuperAdmin))
            {
                return Forbid("المدير لا يمكنه حذف مدير آخر أو مشرف عام.");
            }

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ... (باقي الدوال تبقى كما هي)
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            if (User.HasClaim("is_owner", "true"))
            {
                return Ok(new { Id = 0, Name = _ownerSettings.Name, Email = _ownerSettings.Email, Role = "SuperAdmin", IsOwner = true });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null) return Unauthorized();

            return Ok(new { user.Id, user.Name, user.Email, Role = user.Role.ToString(), IsOwner = false });
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser(UpdateUserInfoDto dto)
        {
            if (User.HasClaim("is_owner", "true"))
            {
                return Forbid("لا يمكن تعديل بيانات المالك من خلال هذه الواجهة.");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            user.Name = dto.Name;
            user.Email = dto.Email;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("me/password")]
        [Authorize]
        public async Task<IActionResult> ChangeCurrentUserPassword(ChangePasswordDto dto)
        {
            if (User.HasClaim("is_owner", "true"))
            {
                return Forbid("لا يمكن تغيير كلمة مرور المالك من خلال هذه الواجهة.");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("كلمة المرور الحالية غير صحيحة.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest("كلمة المرور الجديدة وتأكيدها غير متطابقين.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        private string GenerateOwnerJwtToken()
        {
            var claims = new[]
            {
                new SecurityClaim(JwtRegisteredClaimNames.Sub, "0"),
                new SecurityClaim(ClaimTypes.Name, _ownerSettings.Name),
                new SecurityClaim(ClaimTypes.Email, _ownerSettings.Email),
                new SecurityClaim(ClaimTypes.Role, "SuperAdmin"),
                new SecurityClaim("is_owner", "true")
            };
            return GenerateToken(claims);
        }

        // UsersController.cs

        private string GenerateUserJwtToken(User user)
        {
            Console.WriteLine($"\n--- [DEBUG BACKEND] Generating Token for User: {user.Email} ---");
            Console.WriteLine($"User Role from DB: {user.Role}");

            var claims = new List<SecurityClaim>
    {
        new SecurityClaim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new SecurityClaim(JwtRegisteredClaimNames.UniqueName, user.Name),
        new SecurityClaim(JwtRegisteredClaimNames.Email, user.Email),
        // نستخدم نصاً مباشراً "role" لضمان وصوله بهذا الاسم للفرونت-إند
        new SecurityClaim("role", user.Role.ToString()),
        new SecurityClaim("is_owner", "false")
    };

            // طباعة كل الـ Claims التي ستوضع في التوكن للتحقق
            foreach (var claim in claims)
            {
                Console.WriteLine($"Adding Claim: {claim.Type} = {claim.Value}");
            }

            return GenerateToken(claims);
        }
        private string GenerateToken(IEnumerable<SecurityClaim> claims)
        {
            // --- ✅✅✅ بداية كود التصحيح (Debugging) ✅✅✅ ---
            Console.WriteLine("\n--- [DEBUG] Reading Configuration in GenerateToken ---");
            var jwtKey = _configuration["JWT_KEY"];
            var jwtIssuer = _configuration["JWT_ISSUER"];
            var jwtAudience = _configuration["JWT_AUDIENCE"];
            Console.WriteLine($"JWT_KEY from config: '{jwtKey}'");
            Console.WriteLine($"JWT_ISSUER from config: '{jwtIssuer}'");
            Console.WriteLine($"JWT_AUDIENCE from config: '{jwtAudience}'");
            Console.WriteLine("--- [DEBUG] End of Reading in GenerateToken ---\n");
            // --- نهاية كود التصحيح ---

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                throw new InvalidOperationException("JWT settings are not configured correctly in environment variables.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var allClaims = claims.ToList();
            allClaims.Add(new SecurityClaim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: allClaims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
