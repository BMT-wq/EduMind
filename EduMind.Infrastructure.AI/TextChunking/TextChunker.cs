namespace EduMind.Infrastructure.AI.TextChunking;

/// <summary>
/// Service chia nhỏ văn bản thành các chunk (đoạn) có kích thước kiểm soát được.
///
/// Thuật toán sử dụng: Sliding Window với Overlap
/// ─────────────────────────────────────────────
/// Thay vì cắt văn bản một cách cứng nhắc tại đúng vị trí chunkSize,
/// thuật toán tìm điểm cắt gần nhất là ranh giới câu ('. ', '? ', '! ', '\n')
/// để các chunk có ngữ nghĩa trọn vẹn hơn.
///
/// Cơ chế Overlap:
/// ───────────────
/// Chunk N bắt đầu từ vị trí: (cuối Chunk N-1) - overlapSize
/// Điều này đảm bảo mỗi chunk "nhìn lại" một phần nội dung của chunk trước,
/// tránh mất thông tin tại ranh giới cắt — rất quan trọng với RAG.
/// </summary>
public sealed class TextChunker
{
    // Ký tự đánh dấu ranh giới câu — ưu tiên theo thứ tự này khi tìm điểm cắt
    private static readonly char[] SentenceDelimiters = ['.', '?', '!', '\n'];

    /// <summary>
    /// Chia văn bản thành danh sách các chunk với kích thước và overlap xác định.
    /// </summary>
    /// <param name="text">Văn bản đầu vào (raw text từ PDF)</param>
    /// <param name="chunkSize">Số ký tự tối đa mỗi chunk</param>
    /// <param name="chunkOverlap">Số ký tự chồng lấp giữa hai chunk liền kề</param>
    /// <returns>Danh sách các chuỗi chunk đã được tách</returns>
    public IReadOnlyList<string> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        // Kiểm tra đầu vào hợp lệ
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 100, nameof(chunkSize));
        ArgumentOutOfRangeException.ThrowIfNegative(chunkOverlap, nameof(chunkOverlap));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(chunkOverlap, chunkSize, nameof(chunkOverlap));

        // Chuẩn hóa văn bản: loại bỏ dòng trắng thừa, chuẩn hóa xuống dòng
        var normalizedText = NormalizeText(text);

        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < normalizedText.Length)
        {
            // Tính vị trí kết thúc lý tưởng của chunk hiện tại
            var endPosition = Math.Min(currentPosition + chunkSize, normalizedText.Length);

            // Nếu chưa đến cuối văn bản, tìm điểm cắt thông minh tại ranh giới câu
            if (endPosition < normalizedText.Length)
            {
                endPosition = FindSmartCutPosition(normalizedText, endPosition, chunkSize / 4);
            }

            // Trích xuất chunk và làm sạch khoảng trắng thừa ở đầu/cuối
            var chunk = normalizedText[currentPosition..endPosition].Trim();

            // Chỉ thêm chunk nếu không trống
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Tính vị trí bắt đầu của chunk tiếp theo:
            // Lùi lại "overlapSize" ký tự từ điểm kết thúc để tạo hiệu ứng overlap
            var nextPosition = endPosition - chunkOverlap;

            // Đảm bảo luôn tiến về phía trước (tránh vòng lặp vô hạn)
            if (nextPosition <= currentPosition)
            {
                nextPosition = currentPosition + 1;
            }

            currentPosition = nextPosition;
        }

        return chunks.AsReadOnly();
    }

    /// <summary>
    /// Tìm điểm cắt thông minh gần với vị trí endPosition nhất,
    /// ưu tiên cắt sau ranh giới câu thay vì cắt giữa từ.
    /// </summary>
    /// <param name="text">Văn bản đang xử lý</param>
    /// <param name="endPosition">Vị trí kết thúc lý tưởng</param>
    /// <param name="searchWindow">Số ký tự cho phép tìm kiếm lùi về phía sau</param>
    private static int FindSmartCutPosition(string text, int endPosition, int searchWindow)
    {
        // Giới hạn vùng tìm kiếm: không tìm quá xa về phía trước
        var searchStart = Math.Max(0, endPosition - searchWindow);

        // Tìm ranh giới câu gần nhất (tìm ngược từ endPosition về searchStart)
        for (var i = endPosition; i >= searchStart; i--)
        {
            if (Array.IndexOf(SentenceDelimiters, text[i]) >= 0)
            {
                // Trả về vị trí NGAY SAU ký tự delimeter (bao gồm cả dấu câu vào chunk)
                return Math.Min(i + 1, text.Length);
            }
        }

        // Fallback: không tìm thấy ranh giới câu → cắt tại khoảng trắng gần nhất
        for (var i = endPosition; i >= searchStart; i--)
        {
            if (text[i] == ' ')
            {
                return i + 1;
            }
        }

        // Fallback cuối cùng: cắt cứng tại endPosition
        return endPosition;
    }

    /// <summary>
    /// Chuẩn hóa văn bản: loại bỏ ký tự không cần thiết, chuẩn hóa khoảng trắng.
    /// </summary>
    private static string NormalizeText(string text)
    {
        // Thay thế nhiều dòng trắng liên tiếp bằng một dòng xuống
        var lines = text.Split('\n');
        var nonEmptyLines = lines.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line));
        return string.Join("\n", nonEmptyLines);
    }
}
