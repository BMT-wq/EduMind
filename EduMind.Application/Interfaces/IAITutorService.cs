using EduMind.Application.Models;

namespace EduMind.Application.Interfaces;

/// <summary>
/// Interface chính của AI Tutor Service — định nghĩa hợp đồng (contract)
/// cho toàn bộ luồng RAG (Retrieval-Augmented Generation).
/// Infrastructure.AI sẽ implement interface này.
/// </summary>
public interface IAITutorService
{
    /// <summary>
    /// Nhận câu hỏi từ người dùng và trả về câu trả lời dựa trên tài liệu học tập.
    /// Luồng: Embed câu hỏi → Tìm chunk liên quan (Retrieval) → Tạo prompt → Gọi Gemini AI (Generation).
    /// </summary>
    /// <param name="request">Thông tin câu hỏi và tham số tìm kiếm</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    Task<AiTutorResponse> AskQuestionAsync(AiTutorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nhập một tài liệu PDF vào hệ thống RAG:
    /// Đọc PDF → Chia chunk → Tạo embedding → Lưu vào Vector DB.
    /// </summary>
    /// <param name="pdfStream">Stream của file PDF cần nhập</param>
    /// <param name="fileName">Tên file để đặt metadata</param>
    /// <param name="collectionName">Tên collection trong Vector DB sẽ lưu các chunk</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    Task IngestDocumentAsync(
        Stream pdfStream,
        string fileName,
        string collectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa toàn bộ dữ liệu (chunks, embeddings) của một tài liệu khỏi Vector DB.
    /// </summary>
    Task DeleteDocumentAsync(string fileName, string collectionName, CancellationToken cancellationToken = default);
}
