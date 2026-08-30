using Grpc.Core;

namespace Library.Api.Middleware;

/// <summary>
/// Translates gRPC status codes coming back from the service into HTTP status codes.
/// </summary>
public sealed class RpcExceptionMapper
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RpcExceptionMapper> _logger;

    public RpcExceptionMapper(RequestDelegate next, ILogger<RpcExceptionMapper> logger)
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
        catch (RpcException ex)
        {
            var statusCode = ex.StatusCode switch
            {
                StatusCode.NotFound => StatusCodes.Status404NotFound,
                StatusCode.FailedPrecondition => StatusCodes.Status409Conflict,
                StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unmapped gRPC failure calling the library service");

            await Results.Problem(detail: ex.Status.Detail, statusCode: statusCode).ExecuteAsync(context);
        }
    }
}
