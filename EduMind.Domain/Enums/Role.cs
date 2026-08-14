namespace EduMind.Domain.Enums;

/// <summary>
/// Enum định nghĩa các vai trò người dùng trong hệ thống EduMind.
/// </summary>
public enum Role
{
    /// <summary>Học sinh - vai trò mặc định, người dùng bình thường</summary>
    Student = 0,

    /// <summary>Giáo viên - có quyền tạo bài giảng, quiz, tài liệu</summary>
    Teacher = 1,

    /// <summary>Quản trị viên - có quyền quản lý toàn hệ thống</summary>
    Admin = 2
}
