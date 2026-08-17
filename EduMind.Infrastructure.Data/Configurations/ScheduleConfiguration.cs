using EduMind.Domain.Entities;
using EduMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMind.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình Fluent API cho entity Schedule.
/// Bao gồm: relationship 1 User → nhiều Schedule (UserId là FK).
/// </summary>
public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        // ── Tên bảng ──────────────────────────────────────────────────────────
        builder.ToTable("Schedules");

        // ── Khóa chính ────────────────────────────────────────────────────────
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        // ── Các cột ───────────────────────────────────────────────────────────

        // Title: bắt buộc, tối đa 300 ký tự
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(300);

        // Description: không bắt buộc, tối đa 1000 ký tự
        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        // UserId: bắt buộc (khóa ngoại)
        builder.Property(s => s.UserId)
            .IsRequired();

        // StartTime: bắt buộc, datetime2 — lưu UTC
        builder.Property(s => s.StartTime)
            .IsRequired();

        // EndTime: bắt buộc, datetime2 — lưu UTC
        builder.Property(s => s.EndTime)
            .IsRequired();

        // Status: lưu tên enum dạng string
        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // ProgressPercentage: 0-100, mặc định 0
        builder.Property(s => s.ProgressPercentage)
            .IsRequired()
            .HasDefaultValue(0);

        // LinkedDocumentIds: JSON string — không giới hạn độ dài
        builder.Property(s => s.LinkedDocumentIds);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt);

        // ── Mối quan hệ (Relationship) ────────────────────────────────────────
        // 1 User → nhiều Schedule
        // Khi User bị xóa: Cascade (xóa Schedule theo)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Schedules_Users_UserId");

        // ── Index ────────────────────────────────────────────────────────────
        // Query lịch trình theo UserId và StartTime rất phổ biến
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_Schedules_UserId");

        builder.HasIndex(s => new { s.UserId, s.StartTime })
            .HasDatabaseName("IX_Schedules_UserId_StartTime");
    }
}
