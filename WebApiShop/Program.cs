using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using Entities;
using Repositories;
using Services;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using WebApiShop;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLog();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? string.Empty;
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect("localhost:6380,abortConnect=false,connectTimeout=500"));
}
builder.Services.AddScoped<IProductCacheService, ProductCacheService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPasswordValidityService, PasswordValidityService>();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddTransient<ISiteTypeRepository, SiteTypeRepository>();
builder.Services.AddTransient<ISiteTypeService, SiteTypeService>();

builder.Services.AddScoped<IPlatformRepository, PlatformRepository>();
builder.Services.AddScoped<IPlatformService, PlatformService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();


builder.Services.AddScoped<IBasicSiteRepository, BasicSiteRepository>();
builder.Services.AddScoped<IBasicSiteService, BasicSiteService>();

builder.Services.AddScoped<IMainCategoryRepository, MainCategoryRepository>();
builder.Services.AddScoped<IMainCategoryService, MainCategoryService>();

builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<ISubCategoryService, SubCategoryService>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderPromptBuilder, OrderPromptBuilder>();

builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();

builder.Services.AddScoped<IStatusRepository, StatusRepository>();
builder.Services.AddScoped<IStatusService, StatusService>();

builder.Services.AddScoped<IGemini, Gemini>();
builder.Services.AddScoped<IGeminiPromptsRepository, GeminiPromptsRepository>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IGeminiChatService, GeminiChatService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();

builder.Services.Configure<GeminiSettings>(options =>
{
    options.ApiKey = builder.Configuration["GeminiSettings:ApiKey"]
        ?? builder.Configuration["GEMINI_API_KEY"]
        ?? string.Empty;
});

// ── JWT / Authentication ─────────────────────────────────────────────────────
// Bind configuration – override Jwt:SecretKey in User Secrets / env vars.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtSection = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSection.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSection.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddOpenApi();
builder.Services.AddDbContext<MyShopContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("Home")));
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(Services.Mapper).Assembly);
});
#region FOR ANGULAR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:5000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for the browser to send/receive HttpOnly cookies.
        });
});
#endregion
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MyAPI-V1");
    });
}

app.UseStaticFiles();

app.UseErrorMiddleware();

app.UseRatingMiddleware();

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseJwtMiddleware();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
