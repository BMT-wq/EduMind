using EduMind.Domain.Entities;
using EduMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMind.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình Fluent API cho entity Document.
/// Bao gồm: relationship 1 User → nhiều Document (OwnerId là FK).
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        // ── Tên bảng ──────────────────────────────────────────────────────────
        builder.ToTable("Documents");

        // ── Khóa chính ────────────────────────────────────────────────────────
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        // ── Các cột ───────────────────────────────────────────────────────────

        // Name: bắt buộc, tối đa 500 ký tự (tên file dài)
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(500);

        // Description: không bắt buộc, tối đa 2000 ký tự
        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        // Type: lưu tên enum dạng string để dễ đọc trong DB
        builder.Property(d => d.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Url: bắt buộc, tối đa 2048 ký tự (URL Firebase Storage)
        builder.Property(d => d.Url)
            .IsRequired()
            .HasMaxLength(2048);

        // FileSizeInBytes: bắt buộc, kiểu long
        builder.Property(d => d.FileSizeInBytes)
            .IsRequired();

        // OwnerId: bắt buộc (khóa ngoại)
        builder.Property(d => d.OwnerId)
            .IsRequired();

        // SharedWithUserIds: JSON string — không giới hạn độ dài (nvarchar(max))
        builder.Property(d => d.SharedWithUserIds);

        // IsDeleted: bắt buộc, mặc định false
        builder.Property(d => d.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt);

        // ── Mối quan hệ (Relationship) ────────────────────────────────────────
        // 1 User (Owner) → nhiều Document
        // Khi User bị xóa: Restrict (không cho xóa User còn tài liệu)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Documents_Users_OwnerId");

        // ── Index ────────────────────────────────────────────────────────────
        // Tìm kiếm Document theo OwnerId rất phổ biến → cần index
        builder.HasIndex(d => d.OwnerId)
            .HasDatabaseName("IX_Documents_OwnerId");

        // Hỗ trợ soft delete query (thường filter IsDeleted = false)
        builder.HasIndex(d => d.IsDeleted)
            .HasDatabaseName("IX_Documents_IsDeleted");
    }
}
