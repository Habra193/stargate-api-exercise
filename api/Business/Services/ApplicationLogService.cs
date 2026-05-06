using StargateAPI.Business.Data;

namespace StargateAPI.Business.Services
{
    public interface IApplicationLogService
    {
        Task LogInformationAsync(string operation, string message, CancellationToken cancellationToken);
        Task LogWarningAsync(string operation, string message, CancellationToken cancellationToken);
        Task LogErrorAsync(string operation, string message, Exception exception, CancellationToken cancellationToken);
    }

    public class ApplicationLogService : IApplicationLogService
    {
        private readonly StargateContext _context;

        public ApplicationLogService(StargateContext context)
        {
            _context = context;
        }

        public async Task LogInformationAsync(string operation, string message, CancellationToken cancellationToken)
        {
            await AddLogAsync("Information", operation, message, null, cancellationToken);
        }

        public async Task LogWarningAsync(string operation, string message, CancellationToken cancellationToken)
        {
            await AddLogAsync("Warning", operation, message, null, cancellationToken);
        }

        public async Task LogErrorAsync(string operation, string message, Exception exception, CancellationToken cancellationToken)
        {
            await AddLogAsync("Error", operation, message, exception, cancellationToken);
        }

        private async Task AddLogAsync(
            string logLevel,
            string operation,
            string message,
            Exception? exception,
            CancellationToken cancellationToken)
        {
            _context.ApplicationLogs.Add(new ApplicationLog
            {
                LogLevel = logLevel,
                Operation = operation,
                Message = message,
                ExceptionMessage = exception?.Message,
                ExceptionStackTrace = exception?.StackTrace,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
