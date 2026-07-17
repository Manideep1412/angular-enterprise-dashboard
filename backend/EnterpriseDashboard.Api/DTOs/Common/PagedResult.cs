namespace EnterpriseDashboard.Api.DTOs.Common;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record ApiResponse<T>(bool Success, T? Data, string? Message = null);
public record ApiResponse(bool Success, string? Message = null);
