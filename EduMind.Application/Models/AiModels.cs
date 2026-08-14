// File-scoped namespace theo cú pháp C# 10
namespace EduMind.Application.Models;

// DTO chứa thông tin một đoạn (chunk) văn bản đã được chia nhỏ từ tài liệu gốc
public sealed record DocumentChunk
{
    /// <summary>ID định danh duy nhất của chunk này</summary>
    public required string Id { get; init; }

    /// <summary>Nội dung văn bản của chunk</summary>
    public required string Content { get; init; }

    /// <summary>Tên hoặc đường dẫn tài liệu nguồn</summary>
    public required string SourceDocument { get; init; }

    /// <summary>Số thứ tự của chunk trong tài liệu (tính từ 0)</summary>
    public int ChunkIndex { get; init; }

    /// <summary>Số trang PDF tương ứng (nếu có)</summary>
    public int? PageNumber { get; init; }

    /// <summary>Vector embedding của chunk này (do AI sinh ra)</summary>
    public float[]? Embedding { get; init; }
}

// DTO chứa kết quả trả lời từ AI Tutor
public sealed record AiTutorResponse
{
    /// <summary>Câu trả lời do AI sinh ra</summary>
    public required string Answer { get; init; }

    /// <summary>Danh sách các chunk tài liệu được dùng làm ngữ cảnh (context) để AI trả lời</summary>
    public List<DocumentChunk> RelevantChunks { get; init; } = [];

    /// <summary>Số token đã tiêu thụ (để theo dõi chi phí API)</summary>
    public int? TokensUsed { get; init; }

    /// <summary>Thời điểm AI tạo ra câu trả lời này</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

// DTO chứa kết quả bóc tách từ tài liệu PDF
public sealed record ParsedDocument
{
    /// <summary>Tên file gốc của tài liệu</summary>
    public required string FileName { get; init; }

    /// <summary>Toàn bộ văn bản thô (raw text) đã được trích xuất từ PDF</summary>
    public required string RawText { get; init; }

    /// <summary>Số trang của tài liệu</summary>
    public int TotalPages { get; init; }

    /// <summary>Nội dung văn bản được phân chia theo từng trang</summary>
    public Dictionary<int, string> PageContents { get; init; } = [];
}

// DTO đầu vào khi người dùng đặt câu hỏi cho AI Tutor
public sealed record AiTutorRequest
{
    /// <summary>Câu hỏi từ người dùng</summary>
    public required string Question { get; init; }

    /// <summary>
    /// ID của tập tài liệu (collection) trong Vector DB mà AI sẽ tìm kiếm context.
    /// Mỗi khoá học / môn học có thể có một collection riêng.
    /// </summary>
    public required string CollectionName { get; init; }

    /// <summary>ID của người dùng đang đặt câu hỏi (dùng để cache riêng biệt)</summary>
    public string? UserId { get; init; }

    /// <summary>Số lượng chunk context tối đa truyền vào prompt của AI (mặc định 5)</summary>
    public int TopKChunks { get; init; } = 5;
}
