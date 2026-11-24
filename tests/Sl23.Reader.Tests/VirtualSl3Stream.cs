using System.Buffers.Binary;

namespace Sl23.Reader.Tests;

/// <summary>
/// A read-only, seekable stream of a very large SL3 file that exists only as a rule: <see cref="FrameCount"/> frames of
/// 65,535 bytes each with a 168-byte header, every frame consistent, all other bytes zero.
/// </summary>
internal sealed class VirtualSl3Stream : Stream
{
    public const int FrameBytes = 65535;

    private readonly long length;
    private long position;

    public VirtualSl3Stream(int frameCount)
    {
        FrameCount = frameCount;
        length = 8 + ((long)frameCount * FrameBytes);
    }

    public int FrameCount { get; }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => position;
        set => position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = (int)Math.Min(count, Math.Max(0, length - position));
        for (var i = 0; i < read; i++)
        {
            buffer[offset + i] = ByteAt(position + i);
        }

        position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            _ => length + offset,
        };
        return position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private static byte ByteAt(long at)
    {
        if (at < 8)
        {
            return at == 0 ? (byte)3 : (byte)0;
        }

        var frame = (at - 8) / FrameBytes;
        var inFrame = (int)((at - 8) % FrameBytes);
        Span<byte> header = stackalloc byte[168];
        header.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)(8 + (frame * FrameBytes)));
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], FrameBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], (uint)frame);
        BinaryPrimitives.WriteUInt32LittleEndian(header[44..], FrameBytes - 168);
        return inFrame < 168 ? header[inFrame] : (byte)0;
    }
}
