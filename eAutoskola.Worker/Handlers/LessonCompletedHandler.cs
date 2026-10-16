using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class LessonCompletedHandler : IMessageHandler<LessonCompletedMessage>
{
    private readonly IMailSender _mailSender;

    public LessonCompletedHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(LessonCompletedMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.LessonCompleted(message.ToName, message.Rating);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
