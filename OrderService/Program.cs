using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.Listen(IPAddress.Any, 7195, listenOptions =>
//    {
//        listenOptions.UseHttps("path-to-your-cert.pfx", "cert-password");
//    });
//});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();