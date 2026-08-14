namespace EduMind.Domain.Entities;

using EduMind.Domain.Enums;

/// <summary>
/// Entity User quản lý thông tin người dùng và định danh trong hệ thống EduMind.
/// Mỗi User có một vai trò duy nhất và thông tin cá nhân như email, điều kiện tên.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Tên đầy đủ của người dùng (bắt buộc).
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Địa chỉ email của người dùng (bắt buộc, duy nhất).
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Mật khẩu được mã hóa (hash) của người dùng (bắt buộc).
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Url ảnh đại diện của người dùng (có thể null).
    /// Thường lưu trữ trên Firebase Storage hoặc CDN khác.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Vai trò của người dùng trong hệ thống (Student, Teacher, Admin).
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Trạng thái hoạt động của người dùng (active/inactive).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// </summary>
    protected User() : base()
    {
    }

    /// <summary>
    /// Constructor có tham số để tạo người dùng mới với dữ liệu bắt buộc.
    /// </summary>
    /// <param name="fullName">Tên đầy đủ của người dùng</param>
    /// <param name="email">Địa chỉ email của người dùng</param>
    /// <param name="passwordHash">Mật khẩu đã được mã hóa</param>
    /// <param name="role">Vai trò của người dùng</param>
    public User(string fullName, string email, string passwordHash, Role role) : base()
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Role = role;
        IsActive = true;
        AvatarUrl = null;
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân của người dùng (tên đầy đủ).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="fullName">Tên đầy đủ mới</param>
    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Tên đầy đủ không được để trống", nameof(fullName));

        FullName = fullName;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật ảnh đại diện của người dùng.
    /// Thường được gọi sau khi tải lên ảnh lên Firebase Storage.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="avatarUrl">Đường link ảnh đại diện (có thể null để xóa ảnh)</param>
    public void UpdateAvatarUrl(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdateTimestamp();
    }

    /// <summary>
    /// Thay đổi mật khẩu của người dùng.
    /// Mật khẩu được truyền vào đã phải được mã hóa trước đó.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newPasswordHash">Mật khẩu mới (đã mã hóa)</param>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Mật khẩu không được để trống", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdateTimestamp();
    }

    /// <summary>
    /// Thay đổi vai trò của người dùng.
    /// Thường được gọi bởi Admin khi nâng quyền hoặc hạ quyền người dùng.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newRole">Vai trò mới</param>
    public void ChangeRole(Role newRole)
    {
        Role = newRole;
        UpdateTimestamp();
    }

    /// <summary>
    /// Vô hiệu hóa tài khoản người dùng (khi người dùng bị khóa hoặc xóa).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Kích hoạt lại tài khoản người dùng (nếu tài khoản bị vô hiệu hóa trước đó).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }
}
