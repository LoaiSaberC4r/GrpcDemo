using Grpc.Core;
using OrderService.Protos;

namespace OrderService.Services
{
    public class OrderServiceImpl : OrderService.Protos.OrderService.OrderServiceBase
    {
        public override Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            var orderId = Guid.NewGuid().ToString();
            return Task.FromResult(new CreateOrderResponse
            {
                OrderId = orderId,
                Status = "Created"
            });
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
    }
}