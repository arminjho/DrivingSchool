using eAutoskola.Contracts.Messaging;

namespace eAutoskola.Infrastructure.Messaging;

public interface IEmailPublisher
{
    /// <summary>
    /// Publishes an email to the Worker for asynchronous sending. Returns false (never
    /// throws) on a broker outage - a messaging failure must never break the API request
    /// that triggered it.
    /// </summary>
    Task<bool> PublishAsync(EmailMessage message, CancellationToken cancellationToken);
}
