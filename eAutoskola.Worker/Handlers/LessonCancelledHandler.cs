using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class LessonCancelledHandler : IMessageHandler<LessonCancelledMessage>
{
    private readonly IMailSender _mailSender;

    public LessonCancelledHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(LessonCancelledMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.LessonCancelled(message.ToName, message.Reason);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
