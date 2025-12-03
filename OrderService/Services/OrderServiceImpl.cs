using Grpc.Core;
using OrderService.Protos;

namespace OrderService.Services
{
    public class OrderServiceImpl : OrderService.Protos.OrderService.OrderServiceBase
    {
        private static string GetOrderCorrelationId(ServerCallContext context)
        {
            var correlationId = context.RequestHeaders.FirstOrDefault(x => x.Key == "x-correlation-id")
                ?.Value ?? Guid.NewGuid().ToString();
            return correlationId;
        }

        private static string GetAcceptLanguage(ServerCallContext context)
        {
            var header = context.RequestHeaders.FirstOrDefault(x => x.Key == "accept-language")
                    ?.Value ?? "en";
            return header;
        }

        private static void AddCorrelationIdToTrailers(ServerCallContext context, string correlationId)
        {
            context.ResponseTrailers.Add("x-correlation-id", correlationId);
        }

        public override Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            // 1) قراءة الـ Metadata
            var correlationId = GetOrderCorrelationId(context);
            var acceptLanguage = GetAcceptLanguage(context);

            Console.WriteLine($"[CreateOrder] CorrelationId={correlationId}, AcceptLanguage={acceptLanguage}");

            try
            {
                if (string.IsNullOrEmpty(request.OrderName))
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

                // 2) نضيف CorrelationId فى Trailers
                AddCorrelationIdToTrailers(context, correlationId);

                return Task.FromResult(response);
            }
            catch (RpcException)
            {
                // لو أنت رميت RpcException خلاص
                AddCorrelationIdToTrailers(context, correlationId);
                throw;
            }
            catch (Exception ex)
            {
                // تحويل أى Exception عادى لـ RpcException
                AddCorrelationIdToTrailers(context, correlationId);

                Console.WriteLine($"[CreateOrder][Error] CorrelationId={correlationId}, Exception={ex}");

                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "An unexpected error occurred while creating the order"));
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

        public override async Task<UploadOrdersResponse> UploadOrders(
    IAsyncStreamReader<CreateOrderRequest> requestStream,
    ServerCallContext context)
        {
            var correlationId = GetOrderCorrelationId(context);
            AddCorrelationIdToTrailers(context, correlationId);

            int totalOrders = 0;
            int totalItems = 0;

            try
            {
                await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    totalOrders++;

                    var orderItemsCount = request.Items?.Count ?? 0;
                    totalItems += orderItemsCount;

                    Console.WriteLine($"[UploadOrders] CorrelationId={correlationId}, OrderName={request.OrderName}, Items={orderItemsCount}");

                    // هنا فى الحقيقة هتعمل Save فى DB أو Queue
                    // بس احنا فى المثال هنكتفى بالـ Counters
                }

                return new UploadOrdersResponse
                {
                    TotalOrders = totalOrders,
                    TotalItems = totalItems
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[UploadOrders][Cancelled] CorrelationId={correlationId}");
                throw new RpcException(new Status(
                    StatusCode.Cancelled,
                    "تم إلغاء رفع الأوردرات بواسطة العميل"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UploadOrders][Error] CorrelationId={correlationId}, Exception={ex}");
                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "حدث خطأ أثناء رفع الأوردرات"));
            }
        }

        public override async Task LiveOrders(
    IAsyncStreamReader<LiveOrderClientMessage> requestStream,
    IServerStreamWriter<LiveOrderServerMessage> responseStream,
    ServerCallContext context)
        {
            var correlationId = GetOrderCorrelationId(context);
            AddCorrelationIdToTrailers(context, correlationId);

            Console.WriteLine($"[LiveOrders] Started. CorrelationId={correlationId}");

            try
            {
                await foreach (var clientMsg in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    Console.WriteLine($"[LiveOrders] Client => OrderId={clientMsg.OrderId}, Action={clientMsg.Action}");

                    // مثال: لو Action = Subscribe هنرد بحالة "Created" مؤقتة
                    var response = new LiveOrderServerMessage
                    {
                        OrderId = clientMsg.OrderId,
                        Status = "Created",
                        Message = $"تم استقبال طلب متابعة الأوردر {clientMsg.OrderId}"
                    };

                    await responseStream.WriteAsync(response);

                    // تقدر هنا تحط Logic حقيقى:
                    // - Register subscription
                    // - Push updates من Background job
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[LiveOrders][Cancelled] CorrelationId={correlationId}");
                // من حق السيرفر يكتفى باللوج، والـ Status هيتبعت Cancelled للعميل
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LiveOrders][Error] CorrelationId={correlationId}, Exception={ex}");
                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "حدث خطأ أثناء الـ LiveOrders"));
            }

            Console.WriteLine($"[LiveOrders] Ended. CorrelationId={correlationId}");
        }
    }
}