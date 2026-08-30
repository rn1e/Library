using Grpc.Core;
using Grpc.Core.Interceptors;

using Library.Service.Domain.Exceptions;

namespace Library.Service.Services;

/// <summary>
/// Turns domain exceptions into gRPC statuses in one place.
/// </summary>
public sealed class DomainExceptionInterceptor : Interceptor
{
    private readonly ILogger<DomainExceptionInterceptor> _logger;

    public DomainExceptionInterceptor(ILogger<DomainExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (NoCopiesAvailableException ex)
        {
            _logger.LogInformation("{Method}: {Message}", context.Method, ex.Message);

            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }
}
