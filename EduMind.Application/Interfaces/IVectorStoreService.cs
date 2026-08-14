using EduMind.Application.Models;

namespace EduMind.Application.Interfaces;

/// <summary>
/// Interface định nghĩa hợp đồng tương tác với Vector Database (Qdrant).
/// Chịu trách nhiệm lưu trữ và tìm kiếm các embedding vector.
/// </summary>
public interface IVectorStoreService
{
    /// <summary>
    /// Tạo collection mới trong Qdrant nếu chưa tồn tại.
    /// </summary>
    /// <param name="collectionName">Tên collection cần tạo</param>
    /// <param name="vectorSize">Kích thước vector (phải khớp với model embedding đang dùng)</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    Task EnsureCollectionExistsAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu một danh sách các chunk (đã có embedding) vào Qdrant.
    /// </summary>
    /// <param name="collectionName">Tên collection đích</param>
    /// <param name="chunks">Danh sách chunk cần lưu (mỗi chunk phải có Embedding != null)</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    Task UpsertChunksAsync(string collectionName, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm semantic các chunk gần nhất với query vector.
    /// </summary>
    /// <param name="collectionName">Tên collection cần tìm kiếm</param>
    /// <param name="queryVector">Vector đại diện cho câu hỏi của người dùng</param>
    /// <param name="topK">Số lượng kết quả tối đa trả về</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    Task<IReadOnlyList<DocumentChunk>> SearchSimilarChunksAsync(
        string collectionName,
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tất cả các điểm (points) thuộc về một tài liệu cụ thể khỏi collection.
    /// </summary>
    Task DeleteBySourceDocumentAsync(string collectionName, string sourceDocument, CancellationToken cancellationToken = default);
}
