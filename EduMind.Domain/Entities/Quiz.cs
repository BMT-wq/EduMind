namespace EduMind.Domain.Entities;

/// <summary>
/// Entity Quiz đại diện cho một bài kiểm tra/quiz trong hệ thống EduMind.
/// Chứa danh sách các câu hỏi, thông tin cấu hình, và điểm số tối đa.
/// </summary>
public class Quiz : BaseEntity
{
    /// <summary>
    /// Tiêu đề của bài quiz (bắt buộc).
    /// Ví dụ: "Kiểm tra Toán học - Chương 1"
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về bài quiz.
    /// Ví dụ: "Kiểm tra kiến thức về phương trình bậc nhất"
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Định danh của giáo viên/người tạo bài quiz.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Danh sách ID các câu hỏi trong quiz (định dạng JSON).
    /// Ví dụ: "["question-id-1", "question-id-2", "question-id-3"]"
    /// </summary>
    public string QuestionIds { get; private set; } = string.Empty;

    /// <summary>
    /// Số lượng câu hỏi trong quiz.
    /// Được cập nhật tự động khi thêm/xóa câu hỏi.
    /// </summary>
    public int TotalQuestions { get; private set; }

    /// <summary>
    /// Thời gian tối đa để hoàn thành quiz (tính theo phút).
    /// Null nếu không giới hạn thời gian.
    /// </summary>
    public int? TimeLimit { get; private set; }

    /// <summary>
    /// Điểm tối đa có thể đạt được cho bài quiz.
    /// Tính tổng từ MaxPoints của tất cả các câu hỏi.
    /// </summary>
    public decimal MaxTotalScore { get; private set; }

    /// <summary>
    /// Điểm số tối thiểu cần đạt để vượt qua bài quiz (pass score).
    /// </summary>
    public decimal? PassingScore { get; private set; }

    /// <summary>
    /// Cho phép xem kết quả quiz ngay sau khi nộp bài.
    /// </summary>
    public bool ShowResultsImmediately { get; private set; }

    /// <summary>
    /// Cho phép xem đáp án chi tiết của từng câu hỏi sau khi nộp bài.
    /// </summary>
    public bool ShowAnswersAfterSubmission { get; private set; }

    /// <summary>
    /// Cho phép học viên làm lại bài quiz (multiple attempts).
    /// </summary>
    public bool AllowMultipleAttempts { get; private set; }

    /// <summary>
    /// Số lần tối đa được làm quiz (nếu AllowMultipleAttempts = true).
    /// Null nếu không giới hạn.
    /// </summary>
    public int? MaxAttempts { get; private set; }

    /// <summary>
    /// Trạng thái công bố của quiz (draft, published, archived).
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>
    /// Ngày bắt đầu phát hành quiz cho học viên.
    /// </summary>
    public DateTime? PublishedDate { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// </summary>
    protected Quiz() : base()
    {
    }

    /// <summary>
    /// Constructor có tham số để tạo bài quiz mới.
    /// </summary>
    /// <param name="title">Tiêu đề quiz</param>
    /// <param name="createdByUserId">ID giáo viên tạo quiz</param>
    /// <param name="description">Mô tả quiz (không bắt buộc)</param>
    /// <param name="timeLimit">Thời gian giới hạn (phút, không bắt buộc)</param>
    /// <param name="passingScore">Điểm đạt (không bắt buộc)</param>
    public Quiz(
        string title,
        Guid createdByUserId,
        string? description = null,
        int? timeLimit = null,
        decimal? passingScore = null) : base()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề quiz không được để trống", nameof(title));

        if (timeLimit.HasValue && timeLimit.Value <= 0)
            throw new ArgumentException("Thời gian giới hạn phải lớn hơn 0", nameof(timeLimit));

        if (passingScore.HasValue && passingScore.Value < 0)
            throw new ArgumentException("Điểm đạt không được âm", nameof(passingScore));

        Title = title;
        CreatedByUserId = createdByUserId;
        Description = description;
        TimeLimit = timeLimit;
        PassingScore = passingScore;
        QuestionIds = string.Empty;
        TotalQuestions = 0;
        MaxTotalScore = 0;
        ShowResultsImmediately = true;
        ShowAnswersAfterSubmission = false;
        AllowMultipleAttempts = false;
        MaxAttempts = 1;
        IsPublished = false;
        PublishedDate = null;
    }

    /// <summary>
    /// Thêm một câu hỏi vào bài quiz.
    /// Cập nhật QuestionIds, TotalQuestions, và MaxTotalScore.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="questionId">ID câu hỏi cần thêm</param>
    /// <param name="questionMaxPoints">Điểm tối đa của câu hỏi</param>
    public void AddQuestion(Guid questionId, decimal questionMaxPoints)
    {
        if (questionId == Guid.Empty)
            throw new ArgumentException("ID câu hỏi không hợp lệ", nameof(questionId));

        if (questionMaxPoints <= 0)
            throw new ArgumentException("Điểm tối đa của câu hỏi phải lớn hơn 0", nameof(questionMaxPoints));

        // Nếu QuestionIds rỗng, khởi tạo mảng mới; nếu không, thêm vào danh sách cũ
        if (string.IsNullOrEmpty(QuestionIds))
        {
            QuestionIds = $"[\"{questionId}\"]";
        }
        else
        {
            // Loại bỏ dấu ] ở cuối và thêm ID mới
            QuestionIds = QuestionIds.TrimEnd(']') + $",\"{questionId}\"]";
        }

        TotalQuestions++;
        MaxTotalScore += questionMaxPoints;
        UpdateTimestamp();
    }

    /// <summary>
    /// Xóa câu hỏi khỏi bài quiz.
    /// Cập nhật QuestionIds, TotalQuestions, và MaxTotalScore.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="questionId">ID câu hỏi cần xóa</param>
    /// <param name="questionMaxPoints">Điểm tối đa của câu hỏi</param>
    public void RemoveQuestion(Guid questionId, decimal questionMaxPoints)
    {
        if (!QuestionIds.Contains(questionId.ToString()))
            throw new InvalidOperationException($"Câu hỏi với ID {questionId} không tồn tại trong quiz này");

        // Loại bỏ ID câu hỏi khỏi danh sách JSON
        QuestionIds = QuestionIds.Replace($"\"{questionId}\"", "").Replace(",,", ",").Trim(',');
        if (QuestionIds == "[]" || string.IsNullOrEmpty(QuestionIds.Trim('[', ']')))
        {
            QuestionIds = "[]";
        }

        TotalQuestions--;
        MaxTotalScore -= questionMaxPoints;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản của quiz (tiêu đề, mô tả).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newTitle">Tiêu đề mới</param>
    /// <param name="newDescription">Mô tả mới (có thể null)</param>
    public void UpdateInfo(string newTitle, string? newDescription = null)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Tiêu đề quiz không được để trống", nameof(newTitle));

        Title = newTitle;
        Description = newDescription;
        UpdateTimestamp();
    }

    /// <summary>
    /// Thay đổi cấu hình hiển thị kết quả quiz cho học viên.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="showResults">Có hiển thị kết quả ngay không</param>
    /// <param name="showAnswers">Có hiển thị đáp án chi tiết không</param>
    public void ConfigureResultsDisplay(bool showResults, bool showAnswers)
    {
        ShowResultsImmediately = showResults;
        ShowAnswersAfterSubmission = showAnswers;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cho phép hoặc không cho phép học viên làm lại bài quiz.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="allowMultiple">Cho phép làm lại không</param>
    /// <param name="maxAttempts">Số lần tối đa (null=vô hạn)</param>
    public void ConfigureAttempts(bool allowMultiple, int? maxAttempts = null)
    {
        if (maxAttempts.HasValue && maxAttempts.Value <= 0)
            throw new ArgumentException("Số lần tối đa phải lớn hơn 0", nameof(maxAttempts));

        AllowMultipleAttempts = allowMultiple;
        MaxAttempts = allowMultiple ? maxAttempts : 1;
        UpdateTimestamp();
    }

    /// <summary>
    /// Phát hành bài quiz (từ trạng thái Draft sang Published).
    /// Tự động cập nhật UpdatedAt và PublishedDate.
    /// </summary>
    public void Publish()
    {
        if (IsPublished)
            throw new InvalidOperationException("Bài quiz đã được phát hành từ trước");

        if (TotalQuestions == 0)
            throw new InvalidOperationException("Không thể phát hành quiz khi không có câu hỏi");

        IsPublished = true;
        PublishedDate = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Gỡ phát hành bài quiz (quay lại trạng thái Draft).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Unpublish()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Bài quiz chưa được phát hành");

        IsPublished = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật thời gian giới hạn để làm quiz.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="minutes">Thời gian giới hạn (phút, null = không giới hạn)</param>
    public void UpdateTimeLimit(int? minutes)
    {
        if (minutes.HasValue && minutes.Value <= 0)
            throw new ArgumentException("Thời gian giới hạn phải lớn hơn 0", nameof(minutes));

        TimeLimit = minutes;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật điểm đạt cho bài quiz.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="score">Điểm đạt (null = không yêu cầu điểm tối thiểu)</param>
    public void UpdatePassingScore(decimal? score)
    {
        if (score.HasValue && (score.Value < 0 || score.Value > MaxTotalScore))
            throw new ArgumentException(
                $"Điểm đạt phải nằm trong khoảng 0 đến {MaxTotalScore}",
                nameof(score));

        PassingScore = score;
        UpdateTimestamp();
    }
}
