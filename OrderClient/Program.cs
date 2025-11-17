using Grpc.Core;
using Grpc.Net.Client;
using OrderClient.Protos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
var channel = GrpcChannel.ForAddress("https://localhost:7195");
var client = new OrderService.OrderServiceClient(channel);

try
{
    var deadline = DateTime.UtcNow.AddSeconds(5);
    var createOrderResponse = await client.CreateOrderAsync(
        new CreateOrderRequest
        {
            OrderName = "New Order",
            Items = { new OrderItem { ItemName = "Item1", Quantity = 2 } }
        }, deadline: deadline);
    Console.WriteLine($"Order ID: {createOrderResponse.OrderId}, Status: {createOrderResponse.Status}");
}
catch (RpcException ex)
{
    if (ex.StatusCode == StatusCode.DeadlineExceeded)
    {
        Console.WriteLine("تم تجاوز الموعد النهائي، لم يتم الرد من الخادم.");
    }
    else
    {
        Console.WriteLine($"حدث خطأ أثناء الاتصال بالخادم: {ex.StatusCode} - {ex.Status.Detail}");
    }
}
var streamingCall = client.StreamOrders(new CreateOrderRequest
{
    OrderName = "New Streaming Order",
    Items = { new OrderItem { ItemName = "Item1", Quantity = 1 } }
});
await foreach (var update in streamingCall.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"Order ID: {update.OrderId}, Order Name: {update.OrderName}");
    foreach (var item in update.Items)
    {
        Console.WriteLine($"Item: {item.ItemName}, Quantity: {item.Quantity}");
    }
}
app.Run();