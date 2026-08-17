using EduMind.Application.Interfaces;
using EduMind.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduMind.WebApi.Controllers;

/// <summary>
/// API Controller quản lý các tính năng AI Tutor (RAG Pipeline).
/// Cho phép học sinh hỏi đáp với AI dựa trên tài liệu PDF đã được tải lên và vectorize.
/// </summary>
[ApiController]
[Route("api/v1/ai-tutor")]
[Produces("application/json")]
public class AiTutorController : ControllerBase
{
    private readonly IAITutorService _aiTutorService;
    private readonly ILogger<AiTutorController> _logger;

    public AiTutorController(
        IAITutorService aiTutorService,
        ILogger<AiTutorController> logger)
    {
        _aiTutorService = aiTutorService;
        _logger = logger;
    }

    /// <summary>
    /// 📤 1. Tải lên tài liệu PDF và nạp vào hệ thống RAG (Ingest Document)
    /// </summary>
    /// <remarks>
    /// Luồng xử lý bên dưới:
    /// 1. Bóc tách văn bản thô từ PDF (dùng UglyToad.PdfPig)
    /// 2. Chia nhỏ văn bản thành các đoạn (Chunking với Sliding Window + Overlap)
    /// 3. Tạo Vector Embedding cho từng chunk qua Gemini Embedding API (text-embedding-004)
    /// 4. Lưu vector và metadata vào Qdrant Vector Database
    /// </remarks>
    /// <param name="file">File PDF tài liệu học tập (max 50MB)</param>
    /// <param name="collectionName">Tên bộ sưu tập trong Qdrant (mặc định: edumind-documents)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Ingest tài liệu thành công</response>
    /// <response code="400">File không hợp lệ hoặc không phải định dạng PDF</response>
    [HttpPost("ingest")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestDocument(
        IFormFile file,
        [FromForm] string collectionName = "edumind-documents",
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn một file PDF hợp lệ." });
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Chỉ chấp nhận file định dạng .pdf" });
        }

        _logger.LogInformation("Nhận yêu cầu Ingest file: {FileName} ({Size} bytes)", file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        await _aiTutorService.IngestDocumentAsync(stream, file.FileName, collectionName, cancellationToken);

        return Ok(new
        {
            message = "Tải lên và xử lý tài liệu PDF thành công!",
            fileName = file.FileName,
            collectionName,
            status = "Vectorized & Indexed in Qdrant"
        });
    }

    /// <summary>
    /// 💬 2. Đặt câu hỏi cho AI Tutor (Ask Question - RAG Pipeline)
    /// </summary>
    /// <remarks>
    /// Luồng xử lý RAG đầy đủ:
    /// 1. Kiểm tra Cache Redis (nếu đã từng hỏi -> trả về kết quả ngay lập tức)
    /// 2. Embed câu hỏi thành vector qua Gemini Embedding API
    /// 3. Tìm kiếm K đoạn văn liên quan nhất trong Qdrant Vector DB (Cosine Similarity)
    /// 4. Tổng hợp Prompt gồm Context + Câu hỏi và gửi cho Gemini Chat API (gemini-2.0-flash)
    /// 5. Lưu câu trả lời vào Redis Cache và trả về kết quả
    /// </remarks>
    /// <param name="request">Yêu cầu hỏi đáp (Question, CollectionName, TopKChunks)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">AI Tutor trả lời thành công kèm các đoạn trích dẫn tài liệu</response>
    /// <response code="400">Câu hỏi rỗng hoặc thông số không hợp lệ</response>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AiTutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AskQuestion(
        [FromBody] AiTutorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "Câu hỏi không được để trống." });
        }

        _logger.LogInformation("Nhận câu hỏi từ học sinh: {Question}", request.Question);

        var response = await _aiTutorService.AskQuestionAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 🗑️ 3. Xóa tài liệu khỏi hệ thống Vector Database
    /// </summary>
    /// <param name="fileName">Tên file PDF cần xóa (ví dụ: BaiGiang.pdf)</param>
    /// <param name="collectionName">Tên bộ sưu tập trong Qdrant (mặc định: edumind-documents)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDocument(
        [FromQuery] string fileName,
        [FromQuery] string collectionName = "edumind-documents",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new { message = "Tên file không được để trống." });
        }

        await _aiTutorService.DeleteDocumentAsync(fileName, collectionName, cancellationToken);
        return Ok(new
        {
            message = $"Đã xóa toàn bộ vector chunks của tài liệu '{fileName}' khỏi collection '{collectionName}'."
        });
    }
}
