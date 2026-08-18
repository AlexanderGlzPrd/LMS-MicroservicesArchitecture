namespace PaymentProviderSim.Worker.Time;
internal sealed class MicrosecondTimeProvider(TimeProvider inner) : TimeProvider
{
    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

    public override DateTimeOffset GetUtcNow()
    {
        var now = inner.GetUtcNow();

        return now.AddTicks(-(now.Ticks % TicksPerMicrosecond));
    }

    public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

    public override long TimestampFrequency => inner.TimestampFrequency;

    public override long GetTimestamp() => inner.GetTimestamp();
}