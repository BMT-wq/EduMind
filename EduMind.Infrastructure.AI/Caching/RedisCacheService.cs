using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EduMind.Infrastructure.AI.Caching;

/// <summary>
/// Service cache kết quả AI sử dụng Redis.
///
/// Mục đích: Tránh gọi Gemini API lặp lại cho cùng một câu hỏi + collection,
/// giúp giảm chi phí API và cải thiện thời gian phản hồi.
///
/// Chiến lược cache key:
///   "ai_tutor:{collectionName}:{hash(question)}"
/// → Mỗi collection có không gian cache riêng biệt.
/// → Hash câu hỏi để chuẩn hóa (bỏ qua khoảng trắng thừa, hoa/thường).
/// </summary>
public sealed class RedisCacheService
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _cacheExpiration;

    // Prefix cho tất cả cache key của AI Tutor (dễ xóa batch theo pattern)
    private const string CacheKeyPrefix = "edumind:ai_tutor";

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger,
        TimeSpan cacheExpiration)
    {
        _redisDb = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _cacheExpiration = cacheExpiration;
    }

    /// <summary>
    /// Lấy giá trị từ Redis cache theo key.
    /// Trả về null nếu không có trong cache hoặc cache đã hết hạn.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu cần deserialize</typeparam>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            // Đọc giá trị từ Redis
            var cachedValue = await _redisDb.StringGetAsync(cacheKey);

            if (cachedValue.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);
                return null;
            }

            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);

            // Deserialize JSON string → T (cast rõ ràng sang string để resolve overload)
            return JsonSerializer.Deserialize<T>((string)cachedValue!);
        }
        catch (Exception ex)
        {
            // Lỗi Redis không nên làm gián đoạn luồng chính → log và bỏ qua
            _logger.LogWarning(ex, "Lỗi khi đọc cache từ Redis: {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Lưu giá trị vào Redis cache với thời gian hết hạn (TTL).
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu cần serialize</typeparam>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="value">Giá trị cần lưu</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    public async Task SetAsync<T>(string cacheKey, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            // Serialize T → JSON string để lưu vào Redis
            var jsonValue = JsonSerializer.Serialize(value);

            // Lưu vào Redis với TTL được cấu hình từ AISettings
            await _redisDb.StringSetAsync(cacheKey, jsonValue, _cacheExpiration);

            _logger.LogDebug("Cache SET: {CacheKey} (TTL: {TTL})", cacheKey, _cacheExpiration);
        }
        catch (Exception ex)
        {
            // Lỗi cache không nên làm gián đoạn luồng chính → log và bỏ qua
            _logger.LogWarning(ex, "Lỗi khi ghi cache vào Redis: {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Xóa một cache entry cụ thể theo key.
    /// </summary>
    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redisDb.KeyDeleteAsync(cacheKey);
            _logger.LogDebug("Cache REMOVED: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa cache từ Redis: {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Tạo cache key chuẩn hóa cho AI Tutor từ tên collection và câu hỏi.
    /// Format: "edumind:ai_tutor:{collectionName}:{sha256_hash_of_normalized_question}"
    /// </summary>
    /// <param name="collectionName">Tên collection Vector DB</param>
    /// <param name="question">Câu hỏi của người dùng</param>
    public static string BuildCacheKey(string collectionName, string question)
    {
        // Chuẩn hóa câu hỏi: trim, lowercase, loại bỏ khoảng trắng thừa
        var normalizedQuestion = question.Trim().ToLowerInvariant();

        // Hash câu hỏi bằng SHA256 để tạo key ngắn gọn, tránh key quá dài
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(normalizedQuestion));

        // Chuyển hash bytes → hex string (chỉ lấy 16 bytes đầu = 32 ký tự hex cho đủ unique)
        var hashHex = Convert.ToHexString(hashBytes[..16]).ToLowerInvariant();

        return $"{CacheKeyPrefix}:{collectionName}:{hashHex}";
    }
}
