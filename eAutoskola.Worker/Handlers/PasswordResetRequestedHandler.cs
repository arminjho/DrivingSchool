using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class PasswordResetRequestedHandler : IMessageHandler<PasswordResetRequestedMessage>
{
    private readonly IMailSender _mailSender;

    public PasswordResetRequestedHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(PasswordResetRequestedMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.PasswordResetRequested(message.ToName, message.Code);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
