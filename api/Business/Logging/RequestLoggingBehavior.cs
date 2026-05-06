using MediatR;
using StargateAPI.Business.Services;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Logging
{
    public class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IApplicationLogService _logService;

        public RequestLoggingBehavior(IApplicationLogService logService)
        {
            _logService = logService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var operation = typeof(TRequest).Name;

            try
            {
                var response = await next();

                if (response is BaseResponse baseResponse)
                {
                    if (baseResponse.Success)
                    {
                        await _logService.LogInformationAsync(
                            operation,
                            $"{operation} completed successfully.",
                            cancellationToken);
                    }
                    else
                    {
                        await _logService.LogWarningAsync(
                            operation,
                            $"{operation} completed with response {baseResponse.ResponseCode}: {baseResponse.Message}",
                            cancellationToken);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    operation,
                    $"{operation} failed with an unhandled exception.",
                    ex,
                    cancellationToken);

                throw;
            }
        }
    }
}
