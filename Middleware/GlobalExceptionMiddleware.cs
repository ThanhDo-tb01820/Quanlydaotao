using System.Net;
using System.Text.Json;

namespace CmeTracker.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
        var detail = exception.Message;
        
        if (exception is KeyNotFoundException)
        {
            statusCode = (int)HttpStatusCode.NotFound;
            message = "Không tìm thấy dữ liệu yêu cầu.";
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = (int)HttpStatusCode.Unauthorized;
            message = "Bạn không có quyền truy cập chức năng này.";
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            message = exception.Message;
        }

        context.Response.StatusCode = statusCode;

        var responseModel = new
        {
            status = statusCode,
            message = message,
            detail = exception.Message + "\n" + exception.StackTrace,
            timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(responseModel, options);
        return context.Response.WriteAsync(json);
    }
}
