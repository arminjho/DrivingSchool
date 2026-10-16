using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class LessonRejectedHandler : IMessageHandler<LessonRejectedMessage>
{
    private readonly IMailSender _mailSender;

    public LessonRejectedHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(LessonRejectedMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.LessonRejected(message.ToName, message.Reason);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
