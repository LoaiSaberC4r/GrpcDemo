using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;

namespace OrderService.Infrastructure
{
    public class GrpcLoggingInterceptor : Interceptor
    {
        private readonly ILogger<GrpcLoggingInterceptor> _logger;

        public GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string GetOrCreateCorrelationId(ServerCallContext context)
        {
            var header = context.RequestHeaders
                .FirstOrDefault(h => h.Key == "x-correlation-id")
                ?.Value;

            return string.IsNullOrWhiteSpace(header)
                ? Guid.NewGuid().ToString()
                : header;
        }

        private static void AddCorrelationIdTrailer(ServerCallContext context, string correlationId)
        {
            if (!context.ResponseTrailers.Any(h => h.Key == "x-correlation-id"))
            {
                context.ResponseTrailers.Add("x-correlation-id", correlationId);
            }
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var correlationId = GetOrCreateCorrelationId(context);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting gRPC unary call {Method} CorrelationId={CorrelationId}",
                method, correlationId);

            try
            {
                var response = await continuation(request, context);

                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogInformation(
                    "Finished gRPC unary call {Method} CorrelationId={CorrelationId} Status=OK Duration={ElapsedMs}ms",
                    method, correlationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (RpcException ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogWarning(
                    ex,
                    "gRPC RpcException in {Method} CorrelationId={CorrelationId} Status={StatusCode}",
                    method, correlationId, ex.StatusCode);

                throw; // نسيب الـ Status كما هو
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogError(
                    ex,
                    "Unhandled exception in gRPC call {Method} CorrelationId={CorrelationId}",
                    method, correlationId);

                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "An unexpected error occurred while processing the request"));
            }
        }

        // 🔹 Server Streaming (StreamOrders)
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var correlationId = GetOrCreateCorrelationId(context);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting gRPC server streaming call {Method} CorrelationId={CorrelationId}",
                method, correlationId);

            try
            {
                await continuation(request, responseStream, context);

                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogInformation(
                    "Finished gRPC server streaming call {Method} CorrelationId={CorrelationId} Duration={ElapsedMs}ms",
                    method, correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (RpcException ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogWarning(
                    ex,
                    "gRPC RpcException in streaming call {Method} CorrelationId={CorrelationId} Status={StatusCode}",
                    method, correlationId, ex.StatusCode);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogError(
                    ex,
                    "Unhandled exception in gRPC streaming call {Method} CorrelationId={CorrelationId}",
                    method, correlationId);

                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "An unexpected error occurred while processing the streaming request"));
            }
        }

        // 🔹 Client Streaming (UploadOrders)
        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var correlationId = GetOrCreateCorrelationId(context);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting gRPC client streaming call {Method} CorrelationId={CorrelationId}",
                method, correlationId);

            try
            {
                var response = await continuation(requestStream, context);

                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogInformation(
                    "Finished gRPC client streaming call {Method} CorrelationId={CorrelationId} Duration={ElapsedMs}ms",
                    method, correlationId, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (RpcException ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogWarning(
                    ex,
                    "gRPC RpcException in client streaming call {Method} CorrelationId={CorrelationId} Status={StatusCode}",
                    method, correlationId, ex.StatusCode);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogError(
                    ex,
                    "Unhandled exception in gRPC client streaming call {Method} CorrelationId={CorrelationId}",
                    method, correlationId);

                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "An unexpected error occurred while processing the client streaming request"));
            }
        }

        // 🔹 Bidirectional Streaming (LiveOrders)
        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
            IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var method = context.Method;
            var correlationId = GetOrCreateCorrelationId(context);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting gRPC duplex streaming call {Method} CorrelationId={CorrelationId}",
                method, correlationId);

            try
            {
                await continuation(requestStream, responseStream, context);

                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogInformation(
                    "Finished gRPC duplex streaming call {Method} CorrelationId={CorrelationId} Duration={ElapsedMs}ms",
                    method, correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (RpcException ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogWarning(
                    ex,
                    "gRPC RpcException in duplex streaming call {Method} CorrelationId={CorrelationId} Status={StatusCode}",
                    method, correlationId, ex.StatusCode);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddCorrelationIdTrailer(context, correlationId);

                _logger.LogError(
                    ex,
                    "Unhandled exception in gRPC duplex streaming call {Method} CorrelationId={CorrelationId}",
                    method, correlationId);

                throw new RpcException(new Status(
                    StatusCode.Internal,
                    "An unexpected error occurred while processing the duplex streaming request"));
            }
        }
    }
}