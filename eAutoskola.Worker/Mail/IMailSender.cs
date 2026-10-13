using eAutoskola.Contracts.Messaging;

namespace eAutoskola.Worker.Mail;

public interface IMailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
