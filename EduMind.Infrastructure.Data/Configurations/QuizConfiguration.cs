using EduMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMind.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình Fluent API cho entity Quiz.
/// Bao gồm: relationship 1 User (Teacher) → nhiều Quiz (CreatedByUserId là FK).
/// </summary>
public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        // ── Tên bảng ──────────────────────────────────────────────────────────
        builder.ToTable("Quizzes");

        // ── Khóa chính ────────────────────────────────────────────────────────
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        // ── Các cột ───────────────────────────────────────────────────────────

        // Title: bắt buộc, tối đa 300 ký tự
        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(300);

        // Description: không bắt buộc, tối đa 2000 ký tự
        builder.Property(q => q.Description)
            .HasMaxLength(2000);

        // CreatedByUserId: bắt buộc (FK đến bảng Users)
        builder.Property(q => q.CreatedByUserId)
            .IsRequired();

        // QuestionIds: JSON string lưu mảng Guid — nvarchar(max)
        builder.Property(q => q.QuestionIds)
            .IsRequired()
            .HasDefaultValue(string.Empty);

        // TotalQuestions: bắt buộc, mặc định 0
        builder.Property(q => q.TotalQuestions)
            .IsRequired()
            .HasDefaultValue(0);

        // TimeLimit: không bắt buộc (phút)
        builder.Property(q => q.TimeLimit);

        // MaxTotalScore: kiểu decimal (18, 2) — đủ chứa điểm số
        builder.Property(q => q.MaxTotalScore)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        // PassingScore: không bắt buộc, decimal (18, 2)
        builder.Property(q => q.PassingScore)
            .HasColumnType("decimal(18,2)");

        // Các cờ boolean
        builder.Property(q => q.ShowResultsImmediately)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(q => q.ShowAnswersAfterSubmission)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(q => q.AllowMultipleAttempts)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(q => q.MaxAttempts)
            .HasDefaultValue(1);

        builder.Property(q => q.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        // PublishedDate: không bắt buộc
        builder.Property(q => q.PublishedDate);

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedAt);

        // ── Mối quan hệ (Relationship) ────────────────────────────────────────
        // 1 User (Teacher) → nhiều Quiz
        // Restrict: không xóa User nếu còn Quiz của họ
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(q => q.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Quizzes_Users_CreatedByUserId");

        // ── Index ────────────────────────────────────────────────────────────
        builder.HasIndex(q => q.CreatedByUserId)
            .HasDatabaseName("IX_Quizzes_CreatedByUserId");

        // Tìm kiếm quiz theo trạng thái phát hành
        builder.HasIndex(q => q.IsPublished)
            .HasDatabaseName("IX_Quizzes_IsPublished");
    }
}
