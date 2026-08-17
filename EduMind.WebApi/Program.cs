using EduMind.Infrastructure.AI.DependencyInjection;
using EduMind.Infrastructure.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Đăng ký các Controllers ───────────────────────────────────────────────
builder.Services.AddControllers();

// ── 2. Đăng ký Tầng Infrastructure.AI (Semantic Kernel, Gemini, Qdrant, Redis) ─
builder.Services.AddEduMindAIInfrastructure(builder.Configuration);

// ── 3. Đăng ký Tầng Infrastructure.Data (EF Core, SQL Server) ────────────────
builder.Services.AddDataInfrastructure(builder.Configuration);

// ── 4. Cấu hình Swagger / OpenAPI Documentation ──────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "EduMind - Không gian học tập thông minh (Smart Learning Space)",
        Version = "v1.0",
        Description = """
            ### 🚀 API Documentation & Interactive Testing Console
            
            EduMind là hệ thống hỗ trợ học tập thông minh tích hợp **AI Tutor (RAG Pipeline)** và **EF Core 10 SQL Server Data Infrastructure**.
            
            #### 🧠 Tính năng AI (EduMind.Infrastructure.AI):
            * **PDF Ingestion**: Bóc tách file PDF bằng `PdfPig` + Chunking + Gemini Embedding (`text-embedding-004`) -> Lưu `Qdrant`
            * **RAG Question Answering**: Redis Cache -> Qdrant Vector Search -> Prompt Building -> Gemini Chat Completion (`gemini-2.0-flash`)
            
            #### 💾 Tính năng Dữ liệu (EduMind.Infrastructure.Data):
            * **SQL Server 2022**: EF Core 10 Fluent API mapping 5 thực thể (`User`, `Document`, `Schedule`, `Quiz`, `Question`)
            * **Auto Timestamp**: Tự động cập nhật `UpdatedAt` khi SaveChanges.
            """,
        Contact = new()
        {
            Name = "EduMind Development Team",
            Url = new Uri("https://github.com/BMT-wq/EduMind")
        }
    });

    // Bật XML comments nếu có
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// ── 5. Cấu hình Request Pipeline & Swagger UI ─────────────────────────────
// Kích hoạt Swagger UI cho cả Development và Staging/Production để dễ dàng test
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EduMind Web API v1.0");
    
    // Đặt RoutePrefix = string.Empty giúp Swagger UI mở ngầm định ngay tại trang chủ (http://localhost:port/)
    options.RoutePrefix = string.Empty;
    
    options.DocumentTitle = "EduMind API Console - Swagger UI";
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.DefaultModelsExpandDepth(-1); // Ẩn bớt phần Schemas ở cuối trang cho gọn giao diện
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();