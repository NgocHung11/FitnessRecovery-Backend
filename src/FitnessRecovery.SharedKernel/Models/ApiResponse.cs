namespace FitnessRecovery.SharedKernel.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static ApiResponse CreateSuccess(string? message = null) => 
        new() { Success = true, Message = message };

    public static ApiResponse CreateError(string message, List<string>? errors = null) => 
        new() { Success = false, Message = message, Errors = errors ?? [] };
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> CreateSuccess(T data, string? message = null) => 
        new() { Success = true, Message = message, Data = data };
}
