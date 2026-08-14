using EduMind.Application.Models;

namespace EduMind.Application.Interfaces;

/// <summary>
/// Interface định nghĩa hợp đồng bóc tách nội dung tài liệu.
/// Infrastructure.AI sẽ implement bằng thư viện UglyToad.PdfPig.
/// </summary>
public interface IDocumentParsingService
{
    /// <summary>
    /// Bóc tách toàn bộ văn bản thô từ một file PDF.
    /// </summary>
    /// <param name="pdfStream">Stream của file PDF</param>
    /// <param name="fileName">Tên file gốc (dùng cho metadata)</param>
    /// <param name="cancellationToken">Token hủy tác vụ</param>
    /// <returns>Đối tượng ParsedDocument chứa raw text và nội dung từng trang</returns>
    Task<ParsedDocument> ParsePdfAsync(Stream pdfStream, string fileName, CancellationToken cancellationToken = default);
}
