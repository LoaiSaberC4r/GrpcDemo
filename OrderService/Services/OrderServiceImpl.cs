using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using OrderService.Protos;

namespace OrderService.Services
{
    [Authorize]
    public class OrderServiceImpl : OrderService.Protos.OrderService.OrderServiceBase
    {
        // CreateOrder متاحة لأي مستخدم Authenticated
        public override Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.OrderName))
            {
                throw new RpcException(new Status(
                    StatusCode.InvalidArgument,
                    "Order name is required"));
            }

            var response = new CreateOrderResponse
            {
                OrderId = Guid.NewGuid().ToString(),
                Status = "Created"
            };

            return Task.FromResult(response);
        }

        // GetOrder متاحة للجميع (حتى بدون Auth) كمثال:
        [AllowAnonymous]
        public override Task<GetOrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetOrderResponse
            {
                OrderId = request.OrderId,
                OrderName = "Sample Order",
                Items = { new OrderItem { ItemName = "Item1", Quantity = 2 } }
            });
        }

        // StreamOrders محتاجة Policy معينة (مثلاً Supervisor)
        [Authorize(Policy = "RequireSupervisor")]
        public override async Task StreamOrders(CreateOrderRequest request, IServerStreamWriter<GetOrderResponse> responseStream, ServerCallContext context)
        {
            for (int i = 0; i < 5; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var response = new GetOrderResponse
                {
                    OrderId = Guid.NewGuid().ToString(),
                    OrderName = $"Order {i + 1}",
                    Items = { new OrderItem { ItemName = "Item" + (i + 1), Quantity = 1 } }
                };

                await responseStream.WriteAsync(response);
                await Task.Delay(1000, context.CancellationToken);
            }
        }

        // UploadOrders: يكفي يكون Authenticated
        public override async Task<UploadOrdersResponse> UploadOrders(
            IAsyncStreamReader<CreateOrderRequest> requestStream,
            ServerCallContext context)
        {
            int totalOrders = 0;
            int totalItems = 0;

            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                totalOrders++;
                totalItems += request.Items?.Count ?? 0;
            }

            return new UploadOrdersResponse
            {
                TotalOrders = totalOrders,
                TotalItems = totalItems
            };
        }

        // LiveOrders: برضه ممكن نخليها لسوبرفايزور بس
        [Authorize(Policy = "RequireSupervisor")]
        public override async Task LiveOrders(
            IAsyncStreamReader<LiveOrderClientMessage> requestStream,
            IServerStreamWriter<LiveOrderServerMessage> responseStream,
            ServerCallContext context)
        {
            await foreach (var clientMsg in requestStream.ReadAllAsync(context.CancellationToken))
            {
                var response = new LiveOrderServerMessage
                {
                    OrderId = clientMsg.OrderId,
                    Status = "Created",
                    Message = $"تم استقبال طلب متابعة الأوردر {clientMsg.OrderId}"
                };

                await responseStream.WriteAsync(response);
            }
        }
    }
}