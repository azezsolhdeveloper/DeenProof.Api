using DeenProof.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

// 1. تحميل ملف .env
DotNetEnv.Env.Load(options: DotNetEnv.LoadOptions.TraversePath());

var builder = WebApplication.CreateBuilder(args);

// 2. مسح مصادر الإعدادات الافتراضية
//builder.Configuration.Sources.Clear();

// 3. إضافة متغيرات البيئة كمصدر وحيد
builder.Configuration.AddEnvironmentVariables();

// 4. إنشاء وتسجيل إعدادات المالك
var ownerSettings = new OwnerSettings
{
    Email = builder.Configuration["OWNER_EMAIL"],
    Password = builder.Configuration["OWNER_PASSWORD"],
    Name = builder.Configuration["OWNER_NAME"]
};
builder.Services.AddSingleton(ownerSettings);

// 5. إعداد قاعدة البيانات
var connectionString = builder.Configuration["DB_CONNECTION_STRING"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 6. إعداد الـ Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// --- ✅✅✅ بداية الإصلاح الحقيقي والنهائي ✅✅✅ ---
// 7. إضافة مصادقة JWT مع قراءة مباشرة للمتغيرات
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // نقرأ القيم مباشرة من IConfiguration، الذي يقرأها من متغيرات البيئة
    var jwtKey = builder.Configuration["JWT_KEY"];
    var jwtIssuer = builder.Configuration["JWT_ISSUER"];
    var jwtAudience = builder.Configuration["JWT_AUDIENCE"];

    // التحقق من أن القيم ليست فارغة
    if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
    {
        throw new InvalidOperationException("JWT settings are not configured correctly in environment variables.");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
// --- نهاية الإصلاح ---

// 8. إعدادات Swagger و CORS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextApp",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "https://deen-proof.vercel.app"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
var app = builder.Build();

// 9. إعداد الـ Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowNextApp");

// 10. تفعيل المصادقة والترخيص
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// 11. تعريف كلاس إعدادات المالك
public class OwnerSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
