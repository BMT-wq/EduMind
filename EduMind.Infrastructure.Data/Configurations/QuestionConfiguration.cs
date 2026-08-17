using EduMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMind.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình Fluent API cho entity Question.
/// Question là entity độc lập — không có FK trực tiếp vào Quiz
/// (Quiz lưu QuestionIds dưới dạng JSON để tránh coupling chặt).
/// </summary>
public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        // ── Tên bảng ──────────────────────────────────────────────────────────
        builder.ToTable("Questions");

        // ── Khóa chính ────────────────────────────────────────────────────────
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        // ── Các cột ───────────────────────────────────────────────────────────

        // Content: bắt buộc, nvarchar(max) — câu hỏi có thể rất dài (bao gồm cả HTML/Markdown)
        builder.Property(q => q.Content)
            .IsRequired();

        // QuestionType: bắt buộc, tối đa 100 ký tự
        // Ví dụ: "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay"
        builder.Property(q => q.QuestionType)
            .IsRequired()
            .HasMaxLength(100);

        // Options: JSON string — nvarchar(max)
        builder.Property(q => q.Options)
            .IsRequired();

        // CorrectAnswer: bắt buộc, tối đa 1000 ký tự
        builder.Property(q => q.CorrectAnswer)
            .IsRequired()
            .HasMaxLength(1000);

        // Explanation: không bắt buộc — nvarchar(max) để giải thích chi tiết
        builder.Property(q => q.Explanation);

        // MaxPoints: decimal (10, 2) — điểm số câu hỏi không cần precision cao
        builder.Property(q => q.MaxPoints)
            .IsRequired()
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(1m);

        // DifficultyLevel: 1-4, tối đa là tinyint nhưng dùng int cho đơn giản
        builder.Property(q => q.DifficultyLevel)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedAt);

        // ── Index ────────────────────────────────────────────────────────────
        // Lọc câu hỏi theo loại và mức độ khó
        builder.HasIndex(q => q.QuestionType)
            .HasDatabaseName("IX_Questions_QuestionType");

        builder.HasIndex(q => q.DifficultyLevel)
            .HasDatabaseName("IX_Questions_DifficultyLevel");
    }
}
