using System.Buffers.Binary;

namespace Sl23.Reader.Tests;

/// <summary>
/// A read-only, seekable stream of a very large SL2 file that exists only as a rule: <see cref="FrameCount"/> frames of
/// 65,535 bytes each, every frame consistent, all other bytes zero. It lets a test reach positions past 2 GiB.
/// </summary>
internal sealed class VirtualSl2Stream : Stream
{
    public const int FrameBytes = 65535;

    private readonly long length;
    private long position;

    public VirtualSl2Stream(int frameCount)
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
            return at == 0 ? (byte)2 : (byte)0;
        }

        var frame = (at - 8) / FrameBytes;
        var inFrame = (int)((at - 8) % FrameBytes);
        Span<byte> header = stackalloc byte[144];
        header.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)(8 + (frame * FrameBytes)));
        BinaryPrimitives.WriteUInt16LittleEndian(header[28..], FrameBytes);
        BinaryPrimitives.WriteUInt16LittleEndian(header[34..], FrameBytes - 144);
        BinaryPrimitives.WriteUInt32LittleEndian(header[36..], (uint)frame);
        return inFrame < 144 ? header[inFrame] : (byte)0;
    }
}

/// <summary>A stream that cannot seek, to check that the reader refuses it.</summary>
internal sealed class NonSeekableStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => 0;

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
