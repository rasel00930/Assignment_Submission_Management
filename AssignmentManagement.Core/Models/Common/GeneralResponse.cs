namespace AssignmentManagement.Core.Models.Common;

public sealed class GeneralResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public static GeneralResponse<T> Ok(T? data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static GeneralResponse<T> Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
