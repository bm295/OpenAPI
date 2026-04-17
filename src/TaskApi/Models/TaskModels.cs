namespace TaskApi.Models;

public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}

public sealed record TaskItem(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    DateTimeOffset? DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueAt);

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskStatus? Status,
    DateTimeOffset? DueAt);

public sealed record TaskListResponse(
    IReadOnlyCollection<TaskItem> Data,
    string? NextCursor);

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);

public sealed record ErrorResponse(string Code, string Message, IReadOnlyCollection<ErrorDetail>? Details = null);

public sealed record ErrorDetail(string Field, string Issue);
