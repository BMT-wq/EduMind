namespace EduMind.Domain.Enums;

/// <summary>
/// Enum định nghĩa các trạng thái của một lịch trình học tập.
/// </summary>
public enum ScheduleStatus
{
    /// <summary>Chưa bắt đầu - lịch trình đã được tạo nhưng chưa kích hoạt</summary>
    Pending = 0,

    /// <summary>Đang diễn ra - học viên đang học tập theo lịch trình</summary>
    InProgress = 1,

    /// <summary>Hoàn thành - học viên đã hoàn tất lịch trình</summary>
    Completed = 2,

    /// <summary>Bị hủy - lịch trình bị hủy bỏ trước khi hoàn thành</summary>
    Cancelled = 3
}
