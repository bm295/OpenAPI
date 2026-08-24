using TaskApi.Application.Contracts;

namespace TaskApi.Presentation.Endpoints;

internal static class EndpointValidation
{
    internal static IResult? ValidatePageSize(int? pageSize)
    {
        var effectivePageSize = pageSize.GetValueOrDefault(25);
        return effectivePageSize is < 1 or > 100
            ? Results.BadRequest(new ErrorResponse(
                "INVALID_REQUEST",
                "pageSize must be between 1 and 100",
                new[] { new ErrorDetail("pageSize", "Out of range") }))
            : null;
    }
}
