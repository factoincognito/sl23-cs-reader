using System.Buffers.Binary;

namespace Sl23.Reader.Tests;

/// <summary>One synthetic SL3 frame, built to design section 6.5. A null size or offset means the consistent value.</summary>
internal sealed record Sl3Frame
{
    public ushort Channel { get; init; }

    public int HeaderSize { get; init; } = 168;

    public uint? OffsetField { get; init; }

    public ushort? FrameSize { get; init; }

    public uint? EchoSize { get; init; }

    public byte[] Echo { get; init; } = [];

    public uint Version { get; init; } = 10;

    public ushort PreviousFrameSize { get; init; }

    public uint FrameIndex { get; init; }

    public float Upper { get; init; }

    public float Lower { get; init; } = 100f;

    public float Depth { get; init; } = 10f;

    public float Speed { get; init; }

    public float Course { get; init; }

    public int X { get; init; }

    public int Y { get; init; }

    public ushort Flags { get; init; }

    public uint Time { get; init; }

    public uint[] PreviousOffsets { get; init; } = [0, 0, 0, 0, 0, 0];
}

internal static class Sl3File
{
    public static byte[] Build(params Sl3Frame[] frames) => Build(frames, trailing: 0);

    public static byte[] Build(Sl3Frame[] frames, int trailing)
    {
        using var file = new MemoryStream();
        file.Write([0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        foreach (var frame in frames)
        {
            file.Write(Frame(frame, file.Position));
        }

        file.Write(new byte[trailing]);
        return file.ToArray();
    }

    public static byte[] Frame(Sl3Frame frame, long position)
    {
        var size = (ushort)(frame.HeaderSize + frame.Echo.Length);
        var bytes = new byte[frame.HeaderSize + frame.Echo.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), frame.OffsetField ?? (uint)position);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), frame.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), frame.FrameSize ?? size);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10), frame.PreviousFrameSize);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12), frame.Channel);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), frame.FrameIndex);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(20), frame.Upper);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(24), frame.Lower);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), frame.EchoSize ?? (uint)frame.Echo.Length);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(48), frame.Depth);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84), frame.Speed);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(92), frame.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(96), frame.Y);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(104), frame.Course);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(116), frame.Flags);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(124), frame.Time);
        if (frame.HeaderSize >= 152)
        {
            for (var i = 0; i < 6; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(128 + (4 * i)), frame.PreviousOffsets[i]);
            }
        }

        frame.Echo.CopyTo(bytes, frame.HeaderSize);
        return bytes;
    }
}
