using TaskApi.Models;

namespace TaskApi.Services;

public sealed class TaskRepository
{
    private readonly Dictionary<Guid, TaskItem> _storage = new();

    public TaskListResponse List(TaskStatus? status, int pageSize, string? cursor)
    {
        var query = _storage.Values.AsEnumerable();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        var ordered = query.OrderBy(task => task.CreatedAt).ToList();

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && int.TryParse(cursor, out var parsedCursor))
        {
            startIndex = Math.Max(parsedCursor, 0);
        }

        var page = ordered.Skip(startIndex).Take(pageSize).ToList();
        var nextCursor = startIndex + page.Count < ordered.Count
            ? (startIndex + page.Count).ToString()
            : null;

        return new TaskListResponse(page, nextCursor);
    }

    public TaskItem? Get(Guid id) => _storage.TryGetValue(id, out var task) ? task : null;

    public TaskItem Create(CreateTaskRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem(
            Guid.NewGuid(),
            request.Title.Trim(),
            request.Description?.Trim(),
            TaskStatus.Todo,
            request.DueAt,
            now,
            now);

        _storage[task.Id] = task;
        return task;
    }

    public TaskItem? Update(Guid id, UpdateTaskRequest request)
    {
        if (!_storage.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = existing with
        {
            Title = request.Title?.Trim() ?? existing.Title,
            Description = request.Description?.Trim() ?? existing.Description,
            Status = request.Status ?? existing.Status,
            DueAt = request.DueAt ?? existing.DueAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _storage[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _storage.Remove(id);
}
