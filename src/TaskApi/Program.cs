using System.Text.Json.Serialization;
using TaskApi.Models;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<TaskRepository>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthResponse("ok", DateTimeOffset.UtcNow)))
    .WithName("GetHealth")
    .WithOpenApi();

app.MapGet("/tasks", (TaskStatus? status, int? pageSize, string? cursor, TaskRepository repository) =>
{
    var effectivePageSize = pageSize.GetValueOrDefault(25);
    if (effectivePageSize is < 1 or > 100)
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_REQUEST",
            "pageSize must be between 1 and 100",
            new[] { new ErrorDetail("pageSize", "Out of range") }));
    }

    var response = repository.List(status, effectivePageSize, cursor);
    return Results.Ok(response);
})
.WithName("ListTasks")
.WithOpenApi();

app.MapGet("/tasks/{taskId:guid}", (Guid taskId, TaskRepository repository) =>
{
    var task = repository.Get(taskId);
    return task is null
        ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Task was not found"))
        : Results.Ok(task);
})
.WithName("GetTask")
.WithOpenApi();

app.MapPost("/tasks", (CreateTaskRequest request, TaskRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_REQUEST",
            "title is required",
            new[] { new ErrorDetail("title", "Cannot be empty") }));
    }

    var created = repository.Create(request);
    return Results.Created($"/tasks/{created.Id}", created);
})
.WithName("CreateTask")
.WithOpenApi();

app.MapPatch("/tasks/{taskId:guid}", (Guid taskId, UpdateTaskRequest request, TaskRepository repository) =>
{
    var hasNoChanges = request is { Title: null, Description: null, Status: null, DueAt: null };
    if (hasNoChanges)
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_REQUEST",
            "At least one field must be provided for update"));
    }

    if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_REQUEST",
            "title cannot be blank",
            new[] { new ErrorDetail("title", "Cannot be whitespace") }));
    }

    var updated = repository.Update(taskId, request);
    return updated is null
        ? Results.NotFound(new ErrorResponse("NOT_FOUND", "Task was not found"))
        : Results.Ok(updated);
})
.WithName("UpdateTask")
.WithOpenApi();

app.MapDelete("/tasks/{taskId:guid}", (Guid taskId, TaskRepository repository) =>
    repository.Delete(taskId)
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse("NOT_FOUND", "Task was not found")))
.WithName("DeleteTask")
.WithOpenApi();

app.Run();
