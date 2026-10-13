using Microsoft.Extensions.Logging;

namespace eAutoskola.Worker;

/// <summary>
/// Exponential backoff retry (1s -> 2s -> 4s -> 8s), per upute.md Appendix A.1. Never swallows
/// an error in an empty catch - every retry and the final failure are logged.
/// </summary>
public static class RetryHelper
{
    public static async Task RunWithRetryAsync(
        Func<Task> action,
        ILogger logger,
        int maxAttempts = 4,
        CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    throw; // let the final failure propagate - caller logs and decides what to do
                }

                logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed, retrying in {Delay}", attempt, maxAttempts, delay);
                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }
    }
}
