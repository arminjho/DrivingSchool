namespace eAutoskola.Domain.Messaging;

/// <summary>
/// Generalizes Phase 2's IEmailPublisher to any message contract type (see
/// docs/REFERENCE_ANALYSIS.md §3, design decision 6) - the queue name is derived from
/// TMessage's type name by the implementation. Never throws on a broker outage; returns false
/// so a messaging failure never breaks the API request that triggered it.
/// </summary>
public interface IMessagePublisher
{
    Task<bool> PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class;
}
