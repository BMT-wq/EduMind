using EduMind.Application.Interfaces;
using EduMind.Application.Models;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace EduMind.Infrastructure.AI.DocumentParsing;

/// <summary>
/// Implement IDocumentParsingService bằng thư viện UglyToad.PdfPig.
/// PdfPig là thư viện mã nguồn mở, thuần C#, không phụ thuộc vào thư viện native.
/// Phù hợp với môi trường server Linux/Windows/macOS.
/// </summary>
public sealed class PdfPigParsingService : IDocumentParsingService
{
    private readonly ILogger<PdfPigParsingService> _logger;

    public PdfPigParsingService(ILogger<PdfPigParsingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ParsedDocument> ParsePdfAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu bóc tách PDF: {FileName}", fileName);

        // PdfPig không hỗ trợ async natively → chạy trên ThreadPool để không block thread hiện tại
        return await Task.Run(() => ParsePdfInternal(pdfStream, fileName), cancellationToken);
    }

    /// <summary>
    /// Logic bóc tách PDF chạy đồng bộ (synchronous) bên trong Task.Run.
    /// </summary>
    private ParsedDocument ParsePdfInternal(Stream pdfStream, string fileName)
    {
        // Dictionary lưu nội dung từng trang: key = số trang (1-indexed), value = text
        var pageContents = new Dictionary<int, string>();
        var fullTextBuilder = new System.Text.StringBuilder();

        try
        {
            // Mở tài liệu PDF từ stream
            // PdfDocument implement IDisposable → dùng using để giải phóng tài nguyên
            using var pdfDocument = PdfDocument.Open(pdfStream);

            _logger.LogDebug("PDF {FileName} có {PageCount} trang", fileName, pdfDocument.NumberOfPages);

            // Duyệt từng trang trong tài liệu
            foreach (var page in pdfDocument.GetPages())
            {
                // ── Bóc tách text từng trang ──────────────────────────────────
                // GetWords() trả về các từ đã được PdfPig nhận dạng và sắp xếp theo vị trí
                // Điều này giúp xử lý tốt hơn các PDF có bố cục phức tạp (nhiều cột, bảng...)
                var wordsOnPage = page.GetWords();

                // Ghép các từ lại thành một đoạn văn với khoảng cách hợp lý
                var pageText = BuildPageText(wordsOnPage);

                // Lưu nội dung trang
                pageContents[page.Number] = pageText;

                // Thêm vào full text với dấu phân cách trang
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    fullTextBuilder.AppendLine(pageText);
                    // Thêm dòng trống giữa các trang để phân ranh giới rõ ràng
                    fullTextBuilder.AppendLine();
                }
            }

            var rawText = fullTextBuilder.ToString().Trim();

            _logger.LogInformation(
                "Đã bóc tách xong PDF '{FileName}': {PageCount} trang, {CharCount} ký tự",
                fileName, pdfDocument.NumberOfPages, rawText.Length);

            return new ParsedDocument
            {
                FileName = fileName,
                RawText = rawText,
                TotalPages = pdfDocument.NumberOfPages,
                PageContents = pageContents
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bóc tách PDF '{FileName}'", fileName);
            throw new InvalidOperationException($"Không thể bóc tách file PDF '{fileName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ghép danh sách các từ trên một trang thành một chuỗi văn bản liền mạch.
    /// Sử dụng tọa độ Y (chiều dọc) của từ để phát hiện ranh giới dòng.
    /// </summary>
    /// <param name="words">Danh sách các từ đã được PdfPig nhận dạng</param>
    private static string BuildPageText(IEnumerable<Word> words)
    {
        var textBuilder = new System.Text.StringBuilder();
        double? previousY = null;

        // Ngưỡng khoảng cách Y để xem là xuống dòng mới (đơn vị: points PDF)
        const double LineBreakThreshold = 5.0;

        foreach (var word in words)
        {
            // Lấy tọa độ Y của bounding box của từ
            var currentY = word.BoundingBox.Bottom;

            if (previousY.HasValue)
            {
                // Nếu từ này nằm trên dòng khác với từ trước → thêm xuống dòng
                if (Math.Abs(currentY - previousY.Value) > LineBreakThreshold)
                {
                    textBuilder.AppendLine();
                }
                else
                {
                    // Cùng dòng → thêm khoảng trắng giữa các từ
                    textBuilder.Append(' ');
                }
            }

            textBuilder.Append(word.Text);
            previousY = currentY;
        }

        return textBuilder.ToString();
    }
}
