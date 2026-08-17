using EduMind.Domain.Entities;
using EduMind.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EduMind.Infrastructure.Data.Contexts;

/// <summary>
/// ApplicationDbContext là "cầu nối" duy nhất giữa tầng Infrastructure và SQL Server.
/// Kế thừa DbContext của Entity Framework Core để quản lý toàn bộ vòng đời
/// của các entity (truy vấn, thêm, sửa, xóa) thông qua Unit of Work pattern.
/// </summary>
public class ApplicationDbContext : DbContext
{
    // ═══════════════════════════════════════════════════════════════════════
    //  DbSet — Đại diện cho mỗi bảng (table) trong SQL Server
    //  EF Core sẽ tự động tạo ra các câu lệnh SQL dựa trên các DbSet này.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Bảng Users — quản lý người dùng hệ thống</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Bảng Documents — quản lý tài liệu học tập</summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>Bảng Schedules — quản lý lịch trình học tập</summary>
    public DbSet<Schedule> Schedules => Set<Schedule>();

    /// <summary>Bảng Quizzes — quản lý bài kiểm tra</summary>
    public DbSet<Quiz> Quizzes => Set<Quiz>();

    /// <summary>Bảng Questions — quản lý câu hỏi trong các bài kiểm tra</summary>
    public DbSet<Question> Questions => Set<Question>();

    /// <summary>
    /// Constructor nhận DbContextOptions được inject từ DI Container.
    /// DbContextOptions chứa Connection String và các cài đặt của SQL Server.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OnModelCreating — Cấu hình Fluent API
    //  Đây là nơi DUY NHẤT để cấu hình mapping giữa Entity và Database Schema.
    //  Nguyên tắc: KHÔNG chạm vào code của tầng Domain.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi đè OnModelCreating để cấu hình Fluent API cho tất cả các entity.
    /// EF Core gọi hàm này MỘT LẦN duy nhất khi khởi tạo model (được cache lại).
    /// Sử dụng IEntityTypeConfiguration riêng biệt cho từng entity (SRP - Single Responsibility Principle).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động quét và áp dụng TẤT CẢ các IEntityTypeConfiguration
        // trong assembly hiện tại — không cần đăng ký thủ công từng class
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SaveChangesAsync — Tự động cập nhật Timestamp
    //  Đây là cơ chế "interceptor" trước khi lưu xuống SQL Server.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi đè SaveChangesAsync để TỰ ĐỘNG CẬP NHẬT UpdatedAt trước khi lưu.
    ///
    /// CƠ CHẾ HOẠT ĐỘNG:
    /// 1. EF Core theo dõi trạng thái (State) của từng entity thông qua Change Tracker.
    ///    - EntityState.Added    → Entity mới được thêm vào (INSERT)
    ///    - EntityState.Modified → Entity đang bị sửa đổi (UPDATE)
    ///    - EntityState.Deleted  → Entity bị xóa (DELETE)
    ///
    /// 2. Trước khi gọi base.SaveChangesAsync():
    ///    - Quét tất cả các entity có State = Modified
    ///    - Tìm thuộc tính "UpdatedAt" thông qua Reflection (EF Core metadata)
    ///    - Gán giá trị DateTime.UtcNow — LUÔN dùng UTC để tránh lỗi múi giờ
    ///
    /// 3. Lý do KHÔNG gán UpdatedAt ở tầng Domain:
    ///    - Domain không nên biết về cơ sở hạ tầng (Infrastructure Concern)
    ///    - Giữ Domain thuần khiết, không phụ thuộc vào EF Core
    ///    - Tập trung logic timestamp ở một nơi duy nhất → dễ bảo trì
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Bước 1: Lấy thời điểm hiện tại (UTC) — dùng 1 lần duy nhất để đảm bảo
        // tất cả các entity được cập nhật cùng một mốc thời gian trong một transaction
        var utcNow = DateTime.UtcNow;

        // Bước 2: Quét Change Tracker, tìm tất cả entity kế thừa BaseEntity
        // đang ở trạng thái Modified (đang được sửa đổi trong request hiện tại)
        var modifiedEntries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(entry => entry.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            // Bước 3: Dùng EF Core Metadata API để gán UpdatedAt
            // "Property(nameof(...))" trả về đối tượng PropertyEntry,
            // cho phép thay đổi giá trị trực tiếp mà không cần setter public trong Domain
            entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = utcNow;
        }

        // Bước 4: Gọi hàm SaveChangesAsync gốc để thực sự lưu xuống SQL Server
        // Trả về số lượng bản ghi bị ảnh hưởng (affected rows)
        return await base.SaveChangesAsync(cancellationToken);
    }
}
