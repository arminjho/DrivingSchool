using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace eAutoskola.Worker.Mail;

/// <summary>
/// Real SMTP send via MailKit - the Worker must perform actual asynchronous work, not just
/// log (upute.md §3.2: "Pomoćni servis mora izvršavati stvarne zadatke, a ne samo logiranje").
/// </summary>
public class SmtpMailSender : IMailSender
{
    private readonly SmtpOptions _options;

    public SmtpMailSender(SmtpOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(_options.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart("html") { Text = message.BodyHtml };

        using var client = new SmtpClient();
        var socketOptions = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
