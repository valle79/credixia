using System.Net;
using System.Net.Mail;
using Lending.Application.Abstractions;
using Lending.Infrastructure.Platform;
using Microsoft.Extensions.Logging;

namespace Lending.Infrastructure.Email;

/// <summary>
/// Sends real email through the SMTP configuration stored by the platform administrator.
/// When email is disabled the message is logged instead of silently discarded, keeping
/// local development usable without faking a delivery.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly PlatformSettingsStore _settingsStore;
    private readonly PlatformNotificationService _notificationService;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        PlatformSettingsStore settingsStore,
        PlatformNotificationService notificationService,
        ILogger<SmtpEmailSender> logger)
    {
        _settingsStore = settingsStore;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            await SendCoreAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Email delivery to {Recipient} failed with subject {Subject}.",
                message.To,
                message.Subject);
            await _notificationService.NotifyEmailFailureAsync(
                exception.Message,
                cancellationToken);
        }
    }

    public async Task SendTestAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        await SendCoreAsync(message, cancellationToken);
    }

    private async Task SendCoreAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var configuration = await _settingsStore.GetEmailConfigurationAsync(cancellationToken);
        if (!configuration.IsReady)
        {
            _logger.LogInformation(
                "Email is disabled or incomplete; message to {Recipient} with subject {Subject}: {Body}",
                message.To,
                message.Subject,
                message.TextBody);
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(
                configuration.FromAddress!,
                configuration.FromName,
                System.Text.Encoding.UTF8),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
        };

        mail.To.Add(message.To);

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody,
                    System.Text.Encoding.UTF8,
                    "text/html"));
        }

        using var client = new SmtpClient(configuration.Host, configuration.Port)
        {
            EnableSsl = configuration.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15_000,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(configuration.Username),
        };

        if (!string.IsNullOrWhiteSpace(configuration.Username))
        {
            client.Credentials = new NetworkCredential(
                configuration.Username,
                configuration.Password ?? string.Empty);
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
