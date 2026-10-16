using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class PaymentReceivedHandler : IMessageHandler<PaymentReceivedMessage>
{
    private readonly IMailSender _mailSender;

    public PaymentReceivedHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(PaymentReceivedMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.PaymentReceived(message.ToName, message.Amount);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
