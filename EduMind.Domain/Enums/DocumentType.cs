namespace EduMind.Domain.Enums;

/// <summary>
/// Enum định nghĩa các loại tài liệu được hỗ trợ trong hệ thống EduMind.
/// </summary>
public enum DocumentType
{
    /// <summary>Tài liệu PDF</summary>
    PDF = 0,

    /// <summary>Tài liệu Word (.docx, .doc)</summary>
    Word = 1,

    /// <summary>Ảnh (JPG, PNG, GIF, v.v.)</summary>
    Image = 2,

    /// <summary>Video (MP4, AVI, WebM, v.v.)</summary>
    Video = 3,

    /// <summary>Bảng tính Excel</summary>
    Excel = 4,

    /// <summary>Tệp PowerPoint</summary>
    PowerPoint = 5,

    /// <summary>Các loại tài liệu khác</summary>
    Other = 6
}
