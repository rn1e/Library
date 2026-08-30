using Library.Api.Middleware;
using Library.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpcClient<LibraryService.LibraryServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Library"]!));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RpcExceptionMapper>();

app.MapControllers();

app.Run();
