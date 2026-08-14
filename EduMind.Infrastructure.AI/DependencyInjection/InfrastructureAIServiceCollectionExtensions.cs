using EduMind.Application.Interfaces;
using EduMind.Infrastructure.AI.Caching;
using EduMind.Infrastructure.AI.Configuration;
using EduMind.Infrastructure.AI.DocumentParsing;
using EduMind.Infrastructure.AI.Services;
using EduMind.Infrastructure.AI.TextChunking;
using EduMind.Infrastructure.AI.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using StackExchange.Redis;

namespace EduMind.Infrastructure.AI.DependencyInjection;

/// <summary>
/// Extension methods cho IServiceCollection.
/// Cho phép tầng WebApi đăng ký toàn bộ dịch vụ AI chỉ bằng một dòng:
///   builder.Services.AddEduMindAIInfrastructure(configuration);
///
/// Tuân thủ nguyên tắc Clean Architecture:
///   → Tầng Presentation (WebApi) KHÔNG biết về các class cụ thể trong Infrastructure.AI.
///   → Chỉ biết về Interface từ Application layer.
/// </summary>
public static class InfrastructureAIServiceCollectionExtensions
{
    /// <summary>
    /// Đăng ký toàn bộ các dịch vụ AI Infrastructure vào DI Container.
    /// </summary>
    /// <param name="services">IServiceCollection cần đăng ký</param>
    /// <param name="configuration">IConfiguration để đọc appsettings.json</param>
    /// <returns>IServiceCollection (hỗ trợ method chaining)</returns>
    public static IServiceCollection AddEduMindAIInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 1. Bind và validate cấu hình AISettings từ appsettings.json ───────
        services.AddOptions<AISettings>()
            .Bind(configuration.GetSection(AISettings.SectionName))
            .ValidateOnStart(); // Báo lỗi ngay khi app khởi động nếu thiếu cấu hình

        // Đọc settings trực tiếp để dùng khi cấu hình các client bên dưới
        var aiSettings = configuration
            .GetSection(AISettings.SectionName)
            .Get<AISettings>()
            ?? throw new InvalidOperationException(
                $"Không tìm thấy section '{AISettings.SectionName}' trong appsettings.json. " +
                "Vui lòng kiểm tra file cấu hình.");

        // ── 2. Đăng ký Microsoft Semantic Kernel với Google Gemini ─────────────
        // Kernel là "orchestrator" trung tâm của Semantic Kernel
        services.AddTransient(serviceProvider =>
        {
            var kernelBuilder = Kernel.CreateBuilder();

            // Đăng ký Gemini Chat Completion (dùng để sinh câu trả lời)
            kernelBuilder.AddGoogleAIGeminiChatCompletion(
                modelId: aiSettings.GeminiChatModelId,
                apiKey: aiSettings.GeminiApiKey);

            // Đăng ký Gemini Text Embedding Generator (dùng để tạo vector embedding)
            kernelBuilder.AddGoogleAIEmbeddingGenerator(
                modelId: aiSettings.GeminiEmbeddingModelId,
                apiKey: aiSettings.GeminiApiKey);

            // Semantic Kernel tự động sử dụng ILoggerFactory đã đăng ký trong DI container bên ngoài.
            // Không cần cấu hình Logging riêng bên trong KernelBuilder.

            return kernelBuilder.Build();
        });

        // ── 3. Đăng ký Qdrant Client ───────────────────────────────────────────
        services.AddSingleton(serviceProvider =>
        {
            // QdrantClient kết nối qua gRPC
            var client = new QdrantClient(
                host: aiSettings.QdrantHost,
                port: aiSettings.QdrantPort,
                https: false,               // Dùng HTTP/2 không TLS (phù hợp môi trường nội bộ)
                apiKey: aiSettings.QdrantApiKey);

            return client;
        });

        // ── 4. Đăng ký Redis ConnectionMultiplexer ────────────────────────────
        // Singleton vì ConnectionMultiplexer được thiết kế để tái sử dụng (thread-safe)
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            return ConnectionMultiplexer.Connect(aiSettings.RedisConnectionString);
        });

        // ── 5. Đăng ký RedisCacheService ──────────────────────────────────────
        services.AddSingleton(serviceProvider =>
        {
            var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisCacheService>>();
            var cacheExpiration = TimeSpan.FromMinutes(aiSettings.CacheExpirationMinutes);

            return new RedisCacheService(connectionMultiplexer, logger, cacheExpiration);
        });

        // ── 6. Đăng ký TextChunker ─────────────────────────────────────────────
        // Singleton vì TextChunker là stateless (không có trạng thái nội bộ)
        services.AddSingleton<TextChunker>();

        // ── 7. Đăng ký Document Parsing Service ───────────────────────────────
        // Transient vì PdfPigParsingService không giữ state giữa các request
        services.AddTransient<IDocumentParsingService, PdfPigParsingService>();

        // ── 8. Đăng ký Vector Store Service ───────────────────────────────────
        // Scoped vì QdrantVectorStoreService phụ thuộc vào QdrantClient (Singleton)
        // nhưng nên tạo mới mỗi request để tránh các vấn đề về state
        services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();

        // ── 9. Đăng ký IAITutorService (Main Service) ─────────────────────────
        // Scoped: phù hợp với vòng đời của một HTTP request
        services.AddScoped<IAITutorService, SemanticKernelAITutorService>();

        return services;
    }
}
