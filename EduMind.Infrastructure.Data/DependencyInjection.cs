using EduMind.Infrastructure.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduMind.Infrastructure.Data;

/// <summary>
/// Extension Methods cho IServiceCollection — theo mô hình Clean Architecture.
/// Đây là điểm giao tiếp duy nhất giữa tầng Infrastructure.Data và tầng Presentation.
///
/// NGUYÊN TẮC CLEAN ARCHITECTURE:
/// → Tầng WebApi KHÔNG được tham chiếu trực tiếp ApplicationDbContext.
/// → Tầng WebApi chỉ gọi AddDataInfrastructure() — không biết bên trong dùng SQL Server gì.
/// → Nếu sau này đổi sang PostgreSQL, chỉ cần sửa trong class này, không động vào WebApi.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký toàn bộ các dịch vụ của tầng Infrastructure.Data vào DI Container.
    /// Được gọi từ Program.cs của EduMind.WebApi.
    ///
    /// CÁCH DÙNG Ở Program.cs:
    ///   builder.Services.AddDataInfrastructure(builder.Configuration);
    /// </summary>
    /// <param name="services">IServiceCollection của ứng dụng</param>
    /// <param name="configuration">IConfiguration chứa appsettings.json</param>
    /// <returns>IServiceCollection (hỗ trợ method chaining)</returns>
    public static IServiceCollection AddDataInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Đọc Connection String từ appsettings.json ─────────────────────────
        // Tên key phải khớp với cấu hình trong appsettings.json:
        //   "ConnectionStrings": { "DefaultConnection": "Server=...;Database=EduMind;..." }
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' không được tìm thấy trong appsettings.json. " +
                "Hãy kiểm tra file cấu hình của bạn.");

        // ── Đăng ký ApplicationDbContext với SQL Server ────────────────────────
        // AddDbContext: EF Core DbContext được đăng ký với Scoped lifetime (mặc định).
        //   → Mỗi HTTP Request sẽ tạo ra 1 instance DbContext riêng biệt.
        //   → Đảm bảo mỗi request là 1 Unit of Work độc lập, tránh xung đột dữ liệu.
        //
        // UseSqlServer: cấu hình EF Core dùng SQL Server provider.
        //   → enableRetryOnFailure: tự động retry khi gặp lỗi transient (mất kết nối tạm thời)
        //   → Phù hợp môi trường production với SQL Server trên Azure/cloud.
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Tự động retry tối đa 3 lần khi gặp lỗi kết nối tạm thời
                // Rất hữu ích khi deploy trên Azure SQL hoặc môi trường cloud
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);

                // Migration assembly: quan trọng nếu DbContext và Migration
                // nằm ở assembly khác nhau (ví dụ: dùng WebApi làm startup project)
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

            // Chỉ bật Sensitive Data Logging trong môi trường Development
            // Cảnh báo: TUYỆT ĐỐI không bật trong Production (lộ thông tin nhạy cảm vào log)
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        return services;
    }
}
