using Library.Api.Controllers;
using Library.Contracts;
using Library.Service.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Tests.Integration;

public sealed class ServiceHostFactory : WebApplicationFactory<LibraryGrpcService>
{
    private readonly string _connectionString;

    public ServiceHostFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Library", _connectionString);
    }

    public LibraryService.LibraryServiceClient CreateGrpcClient() =>
        new(Grpc.Net.Client.GrpcChannel.ForAddress(
            Server.BaseAddress,
            new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = Server.CreateHandler() }));
}

public sealed class ApiHostFactory : WebApplicationFactory<BooksController>
{
    private readonly ServiceHostFactory _service;

    public ApiHostFactory(ServiceHostFactory service)
    {
        _service = service;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var handler = _service.Server.CreateHandler();

            services.AddGrpcClient<LibraryService.LibraryServiceClient>(o => o.Address = _service.Server.BaseAddress)
                    .ConfigurePrimaryHttpMessageHandler(() => handler);
        });
    }
}
