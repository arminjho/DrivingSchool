namespace eAutoskola.Worker.Handlers;

/// <summary>Does the REAL work for one message type (upute.md §3.2: "Pomoćni servis mora
/// izvršavati stvarne zadatke, a ne samo logiranje") - resolved per-message from a fresh DI
/// scope by MessageConsumerWorker&lt;TMessage&gt;, which owns all the queue plumbing.</summary>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
