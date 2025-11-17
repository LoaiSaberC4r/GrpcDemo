using Grpc.Core;
using OrderService.Protos;

namespace OrderService.Services
{
    public class OrderServiceImpl : OrderService.Protos.OrderService.OrderServiceBase
    {
        public override Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.OrderName))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Order name cannot be empty"));
                }
                var orderId = Guid.NewGuid().ToString();
                return Task.FromResult(new CreateOrderResponse
                {
                    OrderId = orderId,
                    Status = "Created"
                });
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override Task<GetOrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetOrderResponse
            {
                OrderId = request.OrderId,
                OrderName = "Sample Order",
                Items = { new OrderItem { ItemName = "Item1", Quantity = 2 } }
            });
        }

        public override async Task StreamOrders(CreateOrderRequest request, IServerStreamWriter<GetOrderResponse> responseStream, ServerCallContext context)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                throw new RpcException(new Status(StatusCode.Cancelled, "تم إلغاء العملية من قبل العميل"));
            }
            for (int i = 0; i < 5; i++)
            {
                var response = new GetOrderResponse
                {
                    OrderId = Guid.NewGuid().ToString(),
                    OrderName = $"Order {i + 1}",
                    Items = { new OrderItem { ItemName = "Item" + (i + 1), Quantity = 1 } }
                };

                await responseStream.WriteAsync(response);
                await Task.Delay(1000);
            }
        }
    }
}