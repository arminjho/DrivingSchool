using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Templates;

namespace eAutoskola.Worker.Handlers;

public class LessonConfirmedHandler : IMessageHandler<LessonConfirmedMessage>
{
    private readonly IMailSender _mailSender;

    public LessonConfirmedHandler(IMailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public Task HandleAsync(LessonConfirmedMessage message, CancellationToken cancellationToken)
    {
        var email = EmailTemplates.LessonConfirmed(message.ToName, message.StartsAtUtc);
        return _mailSender.SendAsync(new EmailMessage
        {
            ToEmail = message.ToEmail, ToName = message.ToName, Subject = email.Subject, BodyHtml = email.BodyHtml,
        }, cancellationToken);
    }
}
