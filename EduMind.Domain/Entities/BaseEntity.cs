namespace EduMind.Domain.Entities;

/// <summary>
/// Lớp trừu tượng cơ sở cho tất cả các entity trong hệ thống EduMind.
/// Chứa các thuộc tính chung: Id, CreatedAt, UpdatedAt.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Định danh duy nhất của entity (Guid).
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Thời điểm entity được tạo.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Thời điểm entity được cập nhật lần cuối.
    /// Null nếu entity chưa bao giờ được cập nhật.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// Tự động khởi tạo Id và CreatedAt.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }

    /// <summary>
    /// Cập nhật mốc thời gian chỉnh sửa (UpdatedAt) lên thời gian hiện tại (UTC).
    /// PHẢI được gọi mỗi khi entity được thay đổi.
    /// </summary>
    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
