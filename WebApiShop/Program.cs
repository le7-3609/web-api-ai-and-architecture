using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using Entities;
using Repositories;
using Services;
using System.Text.Json;
using WebApiShop;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseNLog();

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

// Add Authentication services
builder.Services.AddAuthentication();

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
                  .AllowAnyMethod();
        });
});
#endregion
var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
