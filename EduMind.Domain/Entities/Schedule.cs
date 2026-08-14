namespace EduMind.Domain.Entities;

using EduMind.Domain.Enums;

/// <summary>
/// Entity Schedule quản lý lịch trình học tập của một người dùng.
/// Lưu thông tin về thời gian học tập (khối thời gian), trạng thái tiến độ học tập.
/// </summary>
public class Schedule : BaseEntity
{
    /// <summary>
    /// Tên/tiêu đề của lịch trình học tập.
    /// Ví dụ: "Toán học lớp 10 - Buổi 1"
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết về lịch trình học tập.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Định danh của người dùng sở hữu lịch trình này.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Thời điểm bắt đầu của khối thời gian học tập (UTC).
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Thời điểm kết thúc của khối thời gian học tập (UTC).
    /// </summary>
    public DateTime EndTime { get; private set; }

    /// <summary>
    /// Trạng thái của lịch trình (Pending, InProgress, Completed, Cancelled).
    /// </summary>
    public ScheduleStatus Status { get; private set; }

    /// <summary>
    /// Tỷ lệ hoàn thành của lịch trình (từ 0 đến 100).
    /// Được cập nhật khi người dùng hoàn thành các bài tập/quiz.
    /// </summary>
    public int ProgressPercentage { get; private set; }

    /// <summary>
    /// Danh sách ID các tài liệu liên quan đến lịch trình này (định dạng JSON).
    /// Ví dụ: "["doc-id-1", "doc-id-2"]"
    /// </summary>
    public string? LinkedDocumentIds { get; private set; }

    /// <summary>
    /// Constructor mặc định - dành cho ORM (Entity Framework, Dapper, ...).
    /// </summary>
    protected Schedule() : base()
    {
    }

    /// <summary>
    /// Constructor có tham số để tạo lịch trình học tập mới.
    /// </summary>
    /// <param name="title">Tiêu đề lịch trình</param>
    /// <param name="userId">ID người dùng sở hữu</param>
    /// <param name="startTime">Thời điểm bắt đầu</param>
    /// <param name="endTime">Thời điểm kết thúc</param>
    /// <param name="description">Mô tả lịch trình (không bắt buộc)</param>
    public Schedule(
        string title,
        Guid userId,
        DateTime startTime,
        DateTime endTime,
        string? description = null) : base()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề lịch trình không được để trống", nameof(title));

        if (startTime >= endTime)
            throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc", nameof(startTime));

        Title = title;
        UserId = userId;
        StartTime = startTime;
        EndTime = endTime;
        Description = description;
        Status = ScheduleStatus.Pending;
        ProgressPercentage = 0;
        LinkedDocumentIds = null;
    }

    /// <summary>
    /// Thay đổi trạng thái lịch trình từ Pending sang InProgress.
    /// Thường được gọi khi người dùng bắt đầu học tập.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Start()
    {
        if (Status != ScheduleStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể bắt đầu lịch trình ở trạng thái Pending, hiện tại: {Status}");

        Status = ScheduleStatus.InProgress;
        UpdateTimestamp();
    }

    /// <summary>
    /// Thay đổi trạng thái lịch trình thành Completed.
    /// Thường được gọi khi người dùng hoàn thành toàn bộ nội dung lịch trình.
    /// Tự động cập nhật ProgressPercentage thành 100 và UpdatedAt.
    /// </summary>
    public void Complete()
    {
        if (Status == ScheduleStatus.Completed || Status == ScheduleStatus.Cancelled)
            throw new InvalidOperationException($"Không thể hoàn thành lịch trình ở trạng thái: {Status}");

        Status = ScheduleStatus.Completed;
        ProgressPercentage = 100;
        UpdateTimestamp();
    }

    /// <summary>
    /// Hủy lịch trình học tập.
    /// Thường được gọi khi người dùng muốn hủy lịch trình hoặc bị quản trị viên hủy.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void Cancel()
    {
        if (Status == ScheduleStatus.Completed || Status == ScheduleStatus.Cancelled)
            throw new InvalidOperationException($"Không thể hủy lịch trình ở trạng thái: {Status}");

        Status = ScheduleStatus.Cancelled;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật tỷ lệ hoàn thành của lịch trình.
    /// Tỷ lệ phải nằm trong khoảng từ 0 đến 100.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="percentage">Tỷ lệ hoàn thành (0-100)</param>
    public void UpdateProgress(int percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentException("Tỷ lệ hoàn thành phải nằm trong khoảng 0-100", nameof(percentage));

        ProgressPercentage = percentage;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật thông tin cơ bản của lịch trình (tiêu đề, mô tả).
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="title">Tiêu đề mới</param>
    /// <param name="description">Mô tả mới (có thể null)</param>
    public void UpdateInfo(string title, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề lịch trình không được để trống", nameof(title));

        Title = title;
        Description = description;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cập nhật khối thời gian (StartTime, EndTime) của lịch trình.
    /// Chỉ được phép khi lịch trình ở trạng thái Pending.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="newStartTime">Thời điểm bắt đầu mới</param>
    /// <param name="newEndTime">Thời điểm kết thúc mới</param>
    public void RescheduleTimeSlot(DateTime newStartTime, DateTime newEndTime)
    {
        if (Status != ScheduleStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể thay đổi khối thời gian khi lịch trình ở trạng thái Pending");

        if (newStartTime >= newEndTime)
            throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc", nameof(newStartTime));

        StartTime = newStartTime;
        EndTime = newEndTime;
        UpdateTimestamp();
    }

    /// <summary>
    /// Liên kết các tài liệu với lịch trình học tập.
    /// Document IDs được lưu dưới dạng JSON string.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    /// <param name="documentIdsJson">Danh sách ID tài liệu (định dạng JSON)</param>
    public void LinkDocuments(string documentIdsJson)
    {
        if (string.IsNullOrWhiteSpace(documentIdsJson))
            throw new ArgumentException("Danh sách tài liệu không được để trống", nameof(documentIdsJson));

        LinkedDocumentIds = documentIdsJson;
        UpdateTimestamp();
    }

    /// <summary>
    /// Xóa liên kết tài liệu khỏi lịch trình.
    /// Tự động cập nhật UpdatedAt.
    /// </summary>
    public void UnlinkDocuments()
    {
        LinkedDocumentIds = null;
        UpdateTimestamp();
    }
}
