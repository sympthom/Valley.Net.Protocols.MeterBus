namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Result type for M-Bus parsing operations. Replaces null/exception hybrid with explicit success/failure.
/// </summary>
public readonly record struct MBusParseResult<T>
{
    public T? Value { get; }
    public MBusError? Error { get; }
    public bool IsSuccess => Error is null;

    private MBusParseResult(T? value, MBusError? error)
    {
        Value = value;
        Error = error;
    }

    public static MBusParseResult<T> Ok(T value) => new(value, null);
    public static MBusParseResult<T> Fail(MBusError error) => new(default, error);
    public static MBusParseResult<T> Fail(string code, string message) => new(default, new MBusError(code, message));
}

public sealed record MBusError(string Code, string Message);
