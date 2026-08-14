namespace EduMind.Infrastructure.AI.Configuration;

/// <summary>
/// Cấu hình tập trung cho toàn bộ tầng Infrastructure.AI.
/// Được bind từ appsettings.json, section "AISettings".
/// </summary>
public sealed class AISettings
{
    // Tên section trong appsettings.json
    public const string SectionName = "AISettings";

    // ── Gemini AI (Google) ──────────────────────────────────────────────────
    /// <summary>API Key để xác thực với Google Gemini API</summary>
    public string GeminiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model ID của Gemini dùng để sinh câu trả lời (chat completion).
    /// Ví dụ: "gemini-2.0-flash", "gemini-1.5-pro"
    /// </summary>
    public string GeminiChatModelId { get; set; } = "gemini-2.0-flash";

    /// <summary>
    /// Model ID của Gemini dùng để tạo embedding vector.
    /// Ví dụ: "text-embedding-004"
    /// </summary>
    public string GeminiEmbeddingModelId { get; set; } = "text-embedding-004";

    /// <summary>
    /// Kích thước chiều (dimension) của vector được sinh ra bởi GeminiEmbeddingModelId.
    /// "text-embedding-004" mặc định = 768.
    /// Phải khớp với kích thước đã cấu hình trong Qdrant collection.
    /// </summary>
    public ulong EmbeddingDimension { get; set; } = 768;

    // ── Chunking Strategy ───────────────────────────────────────────────────
    /// <summary>
    /// Số ký tự tối đa mỗi chunk văn bản (trước khi gửi vào model embedding).
    /// Mặc định 1000 chars ≈ ~250 tokens — nằm trong giới hạn an toàn.
    /// </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>
    /// Số ký tự chồng lấp (overlap) giữa hai chunk liền kề.
    /// 100 chars = 10% của ChunkSize 1000 — bảo toàn ngữ nghĩa qua ranh giới chunk.
    /// </summary>
    public int ChunkOverlap { get; set; } = 100;

    // ── Qdrant Vector Database ──────────────────────────────────────────────
    /// <summary>Hostname/IP của Qdrant server. Ví dụ: "localhost"</summary>
    public string QdrantHost { get; set; } = "localhost";

    /// <summary>Port gRPC của Qdrant (mặc định: 6334)</summary>
    public int QdrantPort { get; set; } = 6334;

    /// <summary>API Key để xác thực với Qdrant (để trống nếu dùng nội bộ không auth)</summary>
    public string? QdrantApiKey { get; set; }

    // ── Redis Cache ─────────────────────────────────────────────────────────
    /// <summary>
    /// Connection string của Redis.
    /// Ví dụ: "localhost:6379" hoặc "redis-server:6379,password=secret"
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Thời gian sống (TTL) của cache trong Redis (đơn vị: phút).
    /// Sau khoảng thời gian này, cache entry sẽ tự hết hạn.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;
}
