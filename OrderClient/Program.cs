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

var createOrderResponse = await client.CreateOrderAsync(new CreateOrderRequest
{
    OrderName = "New Order",
    Items = { new OrderItem { ItemName = "Item1", Quantity = 2 } }
});

Console.WriteLine($"Order ID: {createOrderResponse.OrderId}, Status: {createOrderResponse.Status}");

var getOrderResponse = await client.GetOrderAsync(new GetOrderRequest { OrderId = createOrderResponse.OrderId });
Console.WriteLine($"Order Name: {getOrderResponse.OrderName}, Item: {getOrderResponse.Items[0].ItemName}");
app.Run();