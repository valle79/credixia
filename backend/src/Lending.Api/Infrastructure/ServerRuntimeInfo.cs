namespace Lending.Api.Infrastructure;

/// <summary>
/// Captures process start time once so the system status endpoint reports real uptime
/// instead of a hard-coded value.
/// </summary>
public sealed class ServerRuntimeInfo
{
    public ServerRuntimeInfo(DateTimeOffset startedAt)
    {
        StartedAt = startedAt;
    }

    public DateTimeOffset StartedAt { get; }
}
