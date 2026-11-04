using Lending.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lending.Infrastructure.Email;

/// <summary>
/// Local development email adapter. It never contacts a provider; the message body is
/// logged so invitation and reset links can be used during development.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Development email to {Recipient} with subject {Subject}: {Body}",
            message.To,
            message.Subject,
            message.TextBody);

        return Task.CompletedTask;
    }
}
