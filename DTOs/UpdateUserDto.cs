using DeenProof.Api.Entities; // تأكد من وجود هذا السطر للوصول إلى UserRole

namespace DeenProof.Api.DTOs
{
    public class UpdateUserDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string? Password { get; set; } // كلمة المرور اختيارية
    }
}
