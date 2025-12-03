using Microsoft.AspNetCore.Authentication.JwtBearer;
using OrderService.Infrastructure;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // دى أمثلة، انت هتظبطها على الـ IdentityProvider بتاعك
        options.Authority = "https://demo-identity-server"; // Issuer
        options.Audience = "orders-api";                    // اسم الـ API
        options.RequireHttpsMetadata = true;                // فى Dev ممكن تخليها false لو شغال بدون https

        // اختياري: لو حابب تضيف Events للـ Logging أو تعديل السلوك
        // options.Events = new JwtBearerEvents { ... };
    });

// ------------------------
// 2) Authorization
// ------------------------
builder.Services.AddAuthorization(options =>
{
    // Policy مثال: محتاجة role = Supervisor
    options.AddPolicy("RequireSupervisor", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Supervisor");
    });
});

// ------------------------
// 3) gRPC + Interceptors
// ------------------------
builder.Services.AddGrpc(options =>
{
    // Interceptor اللى عملناه قبل كده
    options.Interceptors.Add<GrpcLoggingInterceptor>();
});

var app = builder.Build();

// ------------------------
// 4) Middleware Pipeline
// ------------------------
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<OrderServiceImpl>();
    endpoints.MapGet("/", () => "Use a gRPC client to communicate with this service.");
});

app.Run();