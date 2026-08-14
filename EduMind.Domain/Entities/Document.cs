namespace EduMind.Domain.Entities;

using EduMind.Domain.Enums;

/// <summary>
/// Entity Document quản lý các tài liệu được tải lên hệ thống EduMind.
/// Lưu metadata: tên tài liệu, loại file, URL lưu trữ trên Firebase Storage hoặc CDN khác.
/// </summary>
public class Document : BaseEntity
{
    /// <summary>
    /// Tên tài liệu (bắt buộc).
    /// Ví dụ: "Bài giảng Toán học lớp 10.pdf"
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về tài liệu (không bắt buộc).
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Loại tài liệu (PDF, Word, Image, Video, Excel, PowerPoint, Other).
    /// Được lưu dưới dạng Enum để dễ quản lý.
    /// </summary>
    public DocumentType Type { get; private set; }

    /// <summary>
    /// Đường dẫn/URL của tài liệu trên Firebase Storage hoặc cloud storage khác.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>
    /// Kích thước tài liệu tính theo byte.
    /// </summary>
    public long FileSizeInBytes { get; private set; }

    /// <summary>
    /// Định danh người dùng sở hữu/tải lên tài liệu này.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Biểu thức định danh của các người dùng có quyền truy cập tài liệu này (JSON format).
    /// Ví dụ: "["user-id-1", "user-id-2"]"
    /// Null nếu tài liệu là công khai.
    /// </summary>
    public string? SharedWithUserIds { get; private set; }

    /// <summary>
    /// Trạng thái xóa mềm (soft delete) - tài liệu bị xóa nhưng vẫn giữ dữ liệu.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// </summary>
    protected Document() : base()
    {
    }

    /// <summary>
    /// Constructor có tham số để tạo tài liệu mới.
    /// </summary>
    /// <param name="name">Tên tài liệu</param>
    /// <param name="type">Loại tài liệu</param>
    /// <param name="url">Đường dẫn URL tài liệu</param>
    /// <param name="fileSizeInBytes">Kích thước file (byte)</param>
    /// <param name="ownerId">ID người dùng sở hữu</param>
    /// <param name="description">Mô tả tài liệu (không bắt buộc)</param>
    public Document(
        string name,
        DocumentType type,
        string url,
        long fileSizeInBytes,
        Guid ownerId,
        string? description = null) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Url = url ?? throw new ArgumentNullException(nameof(url));
        FileSizeInBytes = fileSizeInBytes;
        OwnerId = ownerId;
        Description = description;
        IsDeleted = false;
        SharedWithUserIds = null;
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản của tài liệu (tên, mô tả).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="name">Tên mới của tài liệu</param>
    /// <param name="description">Mô tả mới (có thể null)</param>
    public void UpdateInfo(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên tài liệu không được để trống", nameof(name));

        Name = name;
        Description = description;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật đường dẫn URL của tài liệu trên Firebase Storage.
    /// Được gọi khi URL được tạo hoặc di chuyển đến một location khác.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newUrl">URL mới của tài liệu</param>
    public void UpdateDocumentUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            throw new ArgumentException("URL tài liệu không được để trống", nameof(newUrl));

        Url = newUrl;
        UpdateTimestamp();
    }

    /// <summary>
    /// Chia sẻ tài liệu với một hoặc nhiều người dùng.
    /// User IDs được lưu dưới dạng JSON string.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="userIdsJson">Danh sách ID người dùng được chia sẻ (định dạng JSON)</param>
    public void ShareWith(string userIdsJson)
    {
        if (string.IsNullOrWhiteSpace(userIdsJson))
            throw new ArgumentException("Danh sách người dùng không được để trống", nameof(userIdsJson));

        SharedWithUserIds = userIdsJson;
        UpdateTimestamp();
    }

    /// <summary>
    /// Thu hồi quyền chia sẻ tài liệu - làm cho tài liệu trở thành riêng tư.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void RevokeSharing()
    {
        SharedWithUserIds = null;
        UpdateTimestamp();
    }

    /// <summary>
    /// Xóa mềm (soft delete) tài liệu.
    /// Tài liệu sẽ không còn hiển thị cho người dùng nhưng dữ liệu vẫn được giữ lại.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Khôi phục tài liệu đã bị xóa mềm.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        UpdateTimestamp();
    }
}
