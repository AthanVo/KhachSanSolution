using KhachSan.Api.Data;
using KhachSan.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using static KhachSan.Api.Service.EmailSetting;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Thêm dịch vụ controllers
builder.Services.AddControllers();

// Cấu hình EmailSettings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Đăng ký EmailService
builder.Services.AddScoped<EmailService>();

// Thêm Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thêm DbContext cho SQL Server
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình CORS từ appsettings
var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "https://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Cấu hình JWT Authentication từ appsettings
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtExpiryMinutes = builder.Configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60);

if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT Key must be at least 32 characters long");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(5) // Cho phép lệch thời gian 5 phút
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError($"JWT Authentication failed: {context.Exception.Message}");

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var message = context.Exception.Message;
            return context.Response.WriteAsync($"{{ \"success\": false, \"message\": \"Authentication failed: {message}\" }}");
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT Token validated successfully");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Cấu hình middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
            var errorMessage = errorFeature?.Error.Message ?? "Lỗi hệ thống.";
            await context.Response.WriteAsync($"{{ \"success\": false, \"message\": \"{errorMessage}\" }}");
        });
    });
}

// Thêm middleware để log requests (hữu ích cho debug CORS)
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path} from Origin: {context.Request.Headers.Origin}");

    // Log các headers quan trọng
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        logger.LogInformation("Authorization header present");
    }

    await next();

    logger.LogInformation($"Response: {context.Response.StatusCode}");
});

// Thứ tự middleware quan trọng
app.UseCors("AllowSpecificOrigin"); // CORS phải đứng trước Authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();