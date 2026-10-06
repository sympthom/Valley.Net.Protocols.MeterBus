namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// The read side of a serial port as the transport's PipeReader sees it. On Windows an async serial
/// read ignores its token and, once the driver ReadTimeout passes without a byte, returns 0, which a
/// PipeReader takes as end of stream: the reply timeout would not fire and the transport would be
/// dead after the first silent slave. Reading again while the port is open turns the driver timeout
/// into a poll interval at which the token is checked. On Unix async reads honour the token and do
/// not return 0 while the port is open, so this only passes the reads through.
/// </summary>
internal sealed class SerialReadStream(Stream inner, Func<bool> isOpen) : Stream
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            if (read > 0 || buffer.IsEmpty || !isOpen())
                return read;

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
