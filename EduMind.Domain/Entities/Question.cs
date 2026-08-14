namespace EduMind.Domain.Entities;

/// <summary>
/// Entity Question đại diện cho một câu hỏi trong quiz.
/// Chứa nội dung câu hỏi, các lựa chọn trả lời, và đáp án đúng.
/// </summary>
public class Question : BaseEntity
{
    /// <summary>
    /// Nội dung của câu hỏi (bắt buộc).
    /// Ví dụ: "2 + 2 = ?"
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Loại câu hỏi (bắt buộc).
    /// Ví dụ: "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay"
    /// </summary>
    public string QuestionType { get; private set; } = string.Empty;

    /// <summary>
    /// Các lựa chọn trả lời cho câu hỏi (định dạng JSON).
    /// Ví dụ: "["A", "B", "C", "D"]" hoặc "["Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D"]"
    /// </summary>
    public string Options { get; private set; } = string.Empty;

    /// <summary>
    /// Đáp án đúng cho câu hỏi (có thể lưu index, letter, hay string tùy type).
    /// Ví dụ: "0" (index 0), "A" (letter A), "Đáp án đúng là..."
    /// </summary>
    public string CorrectAnswer { get; private set; } = string.Empty;

    /// <summary>
    /// Giải thích chi tiết về đáp án đúng - giúp học viên hiểu sâu hơn.
    /// </summary>
    public string? Explanation { get; private set; }

    /// <summary>
    /// Điểm số tối đa cho câu hỏi này (mặc định 1 điểm).
    /// </summary>
    public decimal MaxPoints { get; private set; }

    /// <summary>
    /// Mức độ khó của câu hỏi (Easy=1, Medium=2, Hard=3, VeryHard=4).
    /// Để dễ phân loại và tạo bài kiểm tra có độ khó phù hợp.
    /// </summary>
    public int DifficultyLevel { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// </summary>
    protected Question() : base()
    {
    }

    /// <summary>
    /// Constructor có tham số để tạo câu hỏi mới.
    /// </summary>
    /// <param name="content">Nội dung câu hỏi</param>
    /// <param name="questionType">Loại câu hỏi</param>
    /// <param name="options">Các lựa chọn (JSON format)</param>
    /// <param name="correctAnswer">Đáp án đúng</param>
    /// <param name="explanation">Giải thích đáp án (không bắt buộc)</param>
    /// <param name="maxPoints">Điểm tối đa (mặc định 1)</param>
    /// <param name="difficultyLevel">Mức độ khó (mặc định 2)</param>
    public Question(
        string content,
        string questionType,
        string options,
        string correctAnswer,
        string? explanation = null,
        decimal maxPoints = 1,
        int difficultyLevel = 2) : base()
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Nội dung câu hỏi không được để trống", nameof(content));

        if (string.IsNullOrWhiteSpace(questionType))
            throw new ArgumentException("Loại câu hỏi không được để trống", nameof(questionType));

        if (string.IsNullOrWhiteSpace(options))
            throw new ArgumentException("Các lựa chọn không được để trống", nameof(options));

        if (string.IsNullOrWhiteSpace(correctAnswer))
            throw new ArgumentException("Đáp án đúng không được để trống", nameof(correctAnswer));

        if (maxPoints <= 0)
            throw new ArgumentException("Điểm tối đa phải lớn hơn 0", nameof(maxPoints));

        if (difficultyLevel < 1 || difficultyLevel > 4)
            throw new ArgumentException("Mức độ khó phải nằm trong khoảng 1-4", nameof(difficultyLevel));

        Content = content;
        QuestionType = questionType;
        Options = options;
        CorrectAnswer = correctAnswer;
        Explanation = explanation;
        MaxPoints = maxPoints;
        DifficultyLevel = difficultyLevel;
    }

    /// <summary>
    /// Cập nhật nội dung câu hỏi.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newContent">Nội dung câu hỏi mới</param>
    public void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Nội dung câu hỏi không được để trống", nameof(newContent));

        Content = newContent;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật các lựa chọn trả lời cho câu hỏi.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newOptions">Các lựa chọn mới (JSON format)</param>
    public void UpdateOptions(string newOptions)
    {
        if (string.IsNullOrWhiteSpace(newOptions))
            throw new ArgumentException("Các lựa chọn không được để trống", nameof(newOptions));

        Options = newOptions;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật đáp án đúng và giải thích.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newCorrectAnswer">Đáp án đúng mới</param>
    /// <param name="newExplanation">Giải thích mới (có thể null)</param>
    public void UpdateCorrectAnswer(string newCorrectAnswer, string? newExplanation = null)
    {
        if (string.IsNullOrWhiteSpace(newCorrectAnswer))
            throw new ArgumentException("Đáp án đúng không được để trống", nameof(newCorrectAnswer));

        CorrectAnswer = newCorrectAnswer;
        Explanation = newExplanation;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật mức độ khó của câu hỏi.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newDifficultyLevel">Mức độ khó mới (1-4)</param>
    public void UpdateDifficultyLevel(int newDifficultyLevel)
    {
        if (newDifficultyLevel < 1 || newDifficultyLevel > 4)
            throw new ArgumentException("Mức độ khó phải nằm trong khoảng 1-4", nameof(newDifficultyLevel));

        DifficultyLevel = newDifficultyLevel;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật điểm tố đa cho câu hỏi.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newMaxPoints">Điểm tối đa mới</param>
    public void UpdateMaxPoints(decimal newMaxPoints)
    {
        if (newMaxPoints <= 0)
            throw new ArgumentException("Điểm tối đa phải lớn hơn 0", nameof(newMaxPoints));

        MaxPoints = newMaxPoints;
        UpdateTimestamp();
    }
}
