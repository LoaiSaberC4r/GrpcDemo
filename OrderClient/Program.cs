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

var jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEyMyIsIm5hbWUiOiJMb3VheSIsInJvbGVzIjpbIlN1cGVydmlzb3IiXSwiYXVkIjoib3JkZXJzLWFwaSIsImlzcyI6ImRlbW8taWRlbnRpdHktc2VydmVyIiwiZXhwIjoxOTk5OTk5OTk5fQ.a6z4QqupSqT8xXRBq2eEsvSdWk5F_8vxjRtCBk0T_6o";

var headers = new Metadata
{
    { "Authorization", $"Bearer {jwtToken}" }
};

//**🔸 Unary Call مع JWT + Deadline
try
{
    var deadline = DateTime.UtcNow.AddSeconds(5);

    var createOrderResponse = await client.CreateOrderAsync(
        new CreateOrderRequest
        {
            OrderName = "New Order",
            Items = { new OrderItem { ItemName = "Item1", Quantity = 2 } }
        },
        headers: headers,
        deadline: deadline);

    Console.WriteLine($"[Client] CreateOrder => Order ID: {createOrderResponse.OrderId}, Status: {createOrderResponse.Status}");
}
catch (RpcException ex)
{
    Console.WriteLine($"[Client][CreateOrder] Error: {ex.StatusCode} - {ex.Status.Detail}");
}

//🔸 Server Streaming مع JWT
try
{
    var streamingCall = client.StreamOrders(
        new CreateOrderRequest
        {
            OrderName = "New Streaming Order",
            Items = { new OrderItem { ItemName = "Item1", Quantity = 1 } }
        },
        headers: headers);

    await foreach (var update in streamingCall.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"[Client] StreamOrders => Order ID: {update.OrderId}, Order Name: {update.OrderName}");
    }
}
catch (RpcException ex)
{
    Console.WriteLine($"[Client][StreamOrders] Error: {ex.StatusCode} - {ex.Status.Detail}");
}

//🔸 Client Streaming مع JWT
try
{
    using var uploadCall = client.UploadOrders(headers: headers);

    for (int i = 1; i <= 3; i++)
    {
        var request = new CreateOrderRequest
        {
            OrderName = $"Bulk Order {i}",
            Items =
            {
                new OrderItem { ItemName = "ItemA", Quantity = i },
                new OrderItem { ItemName = "ItemB", Quantity = i + 1 }
            }
        };

        Console.WriteLine($"[Client] UploadOrders => Sending order: {request.OrderName}");
        await uploadCall.RequestStream.WriteAsync(request);
    }

    await uploadCall.RequestStream.CompleteAsync();

    var summary = await uploadCall.ResponseAsync;
    Console.WriteLine($"[Client] UploadOrders Summary => TotalOrders={summary.TotalOrders}, TotalItems={summary.TotalItems}");
}
catch (RpcException ex)
{
    Console.WriteLine($"[Client][UploadOrders] Error: {ex.StatusCode} - {ex.Status.Detail}");
}
//🔸 BiDi Streaming مع JWT
try
{
    using var liveCall = client.LiveOrders(headers: headers);

    var readTask = Task.Run(async () =>
    {
        await foreach (var serverMsg in liveCall.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"[Client][LiveOrders] Server => OrderId={serverMsg.OrderId}, Status={serverMsg.Status}, Message={serverMsg.Message}");
        }
    });

    for (int i = 1; i <= 3; i++)
    {
        var msg = new LiveOrderClientMessage
        {
            OrderId = $"ORD-{i}",
            Action = "Subscribe"
        };

        Console.WriteLine($"[Client][LiveOrders] Sending subscribe for {msg.OrderId}");
        await liveCall.RequestStream.WriteAsync(msg);

        await Task.Delay(500);
    }

    await liveCall.RequestStream.CompleteAsync();
    await readTask;
}
catch (RpcException ex)
{
    Console.WriteLine($"[Client][LiveOrders] Error: {ex.StatusCode} - {ex.Status.Detail}");
}

app.Run();