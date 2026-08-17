using EduMind.Domain.Entities;
using EduMind.Domain.Enums;
using EduMind.Infrastructure.Data.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EduMind.WebApi.Controllers;

/// <summary>
/// API Controller kiểm thử kết nối Database (SQL Server, Redis, Qdrant) và các thao tác CRUD cơ bản qua EF Core.
/// </summary>
[ApiController]
[Route("api/v1/database")]
[Produces("application/json")]
public class DataTestController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DataTestController> _logger;

    public DataTestController(
        ApplicationDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<DataTestController> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// 🛠️ 1. Khởi tạo Schema Database SQL Server (Ensure Created)
    /// </summary>
    /// <remarks>
    /// Tạo tự động tất cả các bảng Users, Documents, Schedules, Quizzes, Questions trên SQL Server 2022 (Docker)
    /// dựa trên Fluent API configurations đã thiết lập.
    /// </remarks>
    [HttpPost("init-schema")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InitDatabaseSchema()
    {
        _logger.LogInformation("Khởi tạo schema database SQL Server...");
        var created = await _dbContext.Database.EnsureCreatedAsync();

        return Ok(new
        {
            message = created ? "Đã tạo mới toàn bộ Schema Database thành công trên SQL Server!" : "Database và Schema đã tồn tại sẵn.",
            tables = new[] { "Users", "Documents", "Schedules", "Quizzes", "Questions" }
        });
    }

    /// <summary>
    /// 🏥 2. Kiểm tra sức khỏe kết nối (Health Check: SQL Server va Redis)
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckHealth()
    {
        bool sqlCanConnect = false;
        string sqlError = string.Empty;

        try
        {
            sqlCanConnect = await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            sqlError = ex.Message;
        }

        bool redisIsConnected = _redis.IsConnected;

        return Ok(new
        {
            status = sqlCanConnect && redisIsConnected ? "Healthy" : "Degraded",
            sqlServer = new
            {
                connected = sqlCanConnect,
                database = _dbContext.Database.GetDbConnection().Database,
                dataSource = _dbContext.Database.GetDbConnection().DataSource,
                error = sqlError
            },
            redis = new
            {
                connected = redisIsConnected,
                endPoints = _redis.GetEndPoints().Select(e => e.ToString())
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 👤 3. Tạo mẫu một Người dùng (User Test) qua EF Core
    /// </summary>
    [HttpPost("users/seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedUser([FromQuery] string fullName = "Nguyễn Văn A", [FromQuery] string email = "student@edumind.edu.vn")
    {
        // Kiểm tra trùng email
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser is not null)
        {
            return Ok(new { message = "User đã tồn tại trong SQL Server.", user = existingUser });
        }

        var user = new User(
            fullName: fullName,
            email: email,
            passwordHash: "$2a$11$e876543210fedcba9876543210fedcba9876543210fedcba9876", // Fake hash
            role: EduMind.Domain.Enums.Role.Student
        );

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(); // Gọi SaveChangesAsync tự động gán UpdatedAt

        return Ok(new
        {
            message = "Tạo User thành công trong SQL Server qua EF Core!",
            user
        });
    }

    /// <summary>
    /// 📋 4. Lấy danh sách Người dùng từ SQL Server
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _dbContext.Users.AsNoTracking().ToListAsync();
        return Ok(new
        {
            total = users.Count,
            users
        });
    }
}
