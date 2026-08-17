using EduMind.Domain.Entities;
using EduMind.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduMind.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình Fluent API cho entity User.
/// Tách ra class riêng theo nguyên tắc SRP — mỗi class chỉ cấu hình 1 entity.
/// EF Core sẽ tự động phát hiện và áp dụng qua ApplyConfigurationsFromAssembly().
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ── Tên bảng ──────────────────────────────────────────────────────────
        builder.ToTable("Users");

        // ── Khóa chính (Primary Key) ──────────────────────────────────────────
        // BaseEntity.Id là Guid — SQL Server sẽ dùng UNIQUEIDENTIFIER
        builder.HasKey(u => u.Id);

        // ── Cấu hình các cột (Columns) ────────────────────────────────────────

        // Id: không cho EF Core tự sinh (vì domain tự tạo Guid.NewGuid() trong constructor)
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        // FullName: bắt buộc, tối đa 200 ký tự
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // Email: bắt buộc, tối đa 256 ký tự (chuẩn RFC 5321), không phân biệt hoa thường
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        // PasswordHash: bắt buộc, tối đa 512 ký tự (đủ chứa Bcrypt hash)
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        // AvatarUrl: không bắt buộc, tối đa 2048 ký tự (URL dài nhất theo chuẩn)
        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(2048);

        // Role: lưu dưới dạng string (tên enum) thay vì int để dễ đọc trong DB
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // IsActive: bắt buộc, giá trị mặc định là true
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // CreatedAt và UpdatedAt: không cho DB tự sinh (domain quản lý)
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        // ── Index ────────────────────────────────────────────────────────────
        // Email phải là duy nhất trong toàn bộ hệ thống
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
    }
}
