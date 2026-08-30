using Library.Service.DataAccess;
using Library.Service.DataAccess.Queries;
using Library.Service.Domain.Lending;
using Library.Service.Domain.Reading;
using Library.Service.Services;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// gRPC needs HTTP/2, and this host serves nothing else. Set in code rather than through
// Kestrel:EndpointDefaults in appsettings, because that section is only applied to
// endpoints declared under Kestrel:Endpoints — an address supplied by ASPNETCORE_URLS
// (launchSettings locally, the container's default port in Docker) never sees it and
// silently falls back to HTTP/1.1.
builder.WebHost.ConfigureKestrel(o => o.ConfigureEndpointDefaults(e => e.Protocols = HttpProtocols.Http2));

builder.Services.AddDbContext<LibraryDbContext>(o =>
    o.UseSqlServer(
        builder.Configuration.GetConnectionString("Library"),
        // In Compose the database accepts connections shortly before it is ready to serve
        // them, so the first query can arrive too early.
        sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<IBorrowingQueries, BorrowingQueries>();

builder.Services.AddScoped<ILoanService, LoanService>();

builder.Services.AddSingleton<ReadingPaceCalculator>();

builder.Services.AddGrpc(o => o.Interceptors.Add<DomainExceptionInterceptor>());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.MapGrpcService<LibraryGrpcService>();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();
    await Seed.ApplyAsync(db);
}

app.Run();
