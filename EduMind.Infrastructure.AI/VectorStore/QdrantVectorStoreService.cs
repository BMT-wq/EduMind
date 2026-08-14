using EduMind.Application.Interfaces;
using EduMind.Application.Models;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace EduMind.Infrastructure.AI.VectorStore;

/// <summary>
/// Implement IVectorStoreService bằng Qdrant — Vector Database hiệu năng cao,
/// hỗ trợ tìm kiếm semantic với nhiều metric khác nhau (Cosine, Dot, Euclidean).
///
/// Mỗi DocumentChunk được lưu vào Qdrant dưới dạng một "Point" gồm:
///   - ID: GUID v5 sinh từ chunk.Id
///   - Vector: float[] embedding từ Gemini
///   - Payload: các metadata (SourceDocument, ChunkIndex, Content, PageNumber)
/// </summary>
public sealed class QdrantVectorStoreService : IVectorStoreService
{
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    // Tên các field trong Payload của Qdrant Point (phải nhất quán khi đọc/ghi)
    private const string PayloadKeyContent = "content";
    private const string PayloadKeySource = "source_document";
    private const string PayloadKeyChunkIndex = "chunk_index";
    private const string PayloadKeyPageNumber = "page_number";

    public QdrantVectorStoreService(QdrantClient qdrantClient, ILogger<QdrantVectorStoreService> logger)
    {
        _qdrantClient = qdrantClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task EnsureCollectionExistsAsync(
        string collectionName,
        ulong vectorSize,
        CancellationToken cancellationToken = default)
    {
        // Kiểm tra collection đã tồn tại chưa để tránh lỗi khi gọi lại nhiều lần
        var collectionExists = await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken);
        if (collectionExists)
        {
            _logger.LogDebug("Collection '{CollectionName}' đã tồn tại trong Qdrant", collectionName);
            return;
        }

        // Tạo collection mới với Cosine Similarity — phù hợp nhất cho text embedding
        await _qdrantClient.CreateCollectionAsync(
            collectionName,
            new VectorParams
            {
                // Kích thước vector phải khớp với Gemini embedding model
                Size = vectorSize,
                // Cosine Distance là metric chuẩn cho semantic similarity với text
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã tạo Qdrant collection '{CollectionName}' với vector size = {VectorSize}",
            collectionName, vectorSize);
    }

    /// <inheritdoc/>
    public async Task UpsertChunksAsync(
        string collectionName,
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        if (chunkList.Count == 0) return;

        // Chuyển từng DocumentChunk → PointStruct (định dạng Qdrant hiểu được)
        var points = chunkList
            .Where(chunk => chunk.Embedding is { Length: > 0 })   // Chỉ lưu chunk đã có embedding
            .Select(chunk => new PointStruct
            {
                // Tạo UUID từ chunk.Id để đảm bảo idempotent (upsert sẽ ghi đè nếu trùng)
                Id = new PointId { Uuid = GenerateDeterministicGuid(chunk.Id).ToString() },
                Vectors = chunk.Embedding!,
                Payload =
                {
                    // Lưu toàn bộ metadata vào Payload để có thể khôi phục chunk khi tìm kiếm
                    [PayloadKeyContent] = chunk.Content,
                    [PayloadKeySource] = chunk.SourceDocument,
                    [PayloadKeyChunkIndex] = chunk.ChunkIndex,
                    [PayloadKeyPageNumber] = chunk.PageNumber.HasValue ? chunk.PageNumber.Value : 0
                }
            })
            .ToList();

        if (points.Count == 0)
        {
            _logger.LogWarning("Không có chunk nào có embedding hợp lệ để lưu vào Qdrant");
            return;
        }

        // Upsert theo batch để tối ưu hiệu năng (Qdrant hỗ trợ upsert nhiều điểm cùng lúc)
        await _qdrantClient.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã upsert {Count} chunks vào Qdrant collection '{CollectionName}'",
            points.Count, collectionName);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentChunk>> SearchSimilarChunksAsync(
        string collectionName,
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        // Tìm kiếm K điểm gần nhất với query vector trong không gian embedding (dùng QueryAsync từ Qdrant.Client 1.19+)
        var searchResults = await _qdrantClient.QueryAsync(
            collectionName,
            queryVector,
            limit: (ulong)topK,
            payloadSelector: true,          // Trả về Payload (metadata) để khôi phục DocumentChunk
            vectorsSelector: false,          // Không cần trả về vector để tiết kiệm băng thông
            cancellationToken: cancellationToken);

        // Chuyển kết quả Qdrant → DocumentChunk
        var chunks = searchResults
            .Select(result => new DocumentChunk
            {
                Id = result.Id.ToString()!,
                Content = result.Payload.TryGetValue(PayloadKeyContent, out var content)
                    ? content.StringValue
                    : string.Empty,
                SourceDocument = result.Payload.TryGetValue(PayloadKeySource, out var source)
                    ? source.StringValue
                    : string.Empty,
                ChunkIndex = result.Payload.TryGetValue(PayloadKeyChunkIndex, out var idx)
                    ? (int)idx.IntegerValue
                    : 0,
                PageNumber = result.Payload.TryGetValue(PayloadKeyPageNumber, out var page)
                    ? (int)page.IntegerValue
                    : null
            })
            .ToList();

        _logger.LogDebug(
            "Tìm kiếm trong collection '{CollectionName}': tìm thấy {Count} chunk liên quan",
            collectionName, chunks.Count);

        return chunks.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task DeleteBySourceDocumentAsync(
        string collectionName,
        string sourceDocument,
        CancellationToken cancellationToken = default)
    {
        // Xóa tất cả points có payload "source_document" khớp với sourceDocument
        // Sử dụng Filter để lọc theo payload — không cần biết ID cụ thể
        await _qdrantClient.DeleteAsync(
            collectionName,
            new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = PayloadKeySource,
                            Match = new Match { Text = sourceDocument }
                        }
                    }
                }
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã xóa tất cả chunks của tài liệu '{SourceDocument}' khỏi collection '{CollectionName}'",
            sourceDocument, collectionName);
    }

    /// <summary>
    /// Sinh GUID xác định (deterministic) từ một chuỗi string.
    /// Dùng để tạo Qdrant Point ID idempotent: cùng chunk.Id → cùng GUID → upsert an toàn.
    /// </summary>
    private static Guid GenerateDeterministicGuid(string input)
    {
        // Dùng MD5 để hash chuỗi input → lấy 16 bytes đầu tạo thành GUID
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
