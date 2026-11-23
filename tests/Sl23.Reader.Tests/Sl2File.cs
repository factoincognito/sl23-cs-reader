using System.Buffers.Binary;

namespace Sl23.Reader.Tests;

/// <summary>One synthetic SL2 frame, built to design section 6.4. A null size or offset means the consistent value.</summary>
internal sealed record Sl2Frame
{
    public ushort Channel { get; init; }

    public uint? OffsetField { get; init; }

    public ushort? FrameSize { get; init; }

    public ushort? EchoSize { get; init; }

    public byte[] Echo { get; init; } = [];

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
}

internal static class Sl2File
{
    public static byte[] Build(params Sl2Frame[] frames) => Build(frames, trailing: 0);

    public static byte[] Build(Sl2Frame[] frames, int trailing)
    {
        using var file = new MemoryStream();
        file.Write([0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        foreach (var frame in frames)
        {
            file.Write(Frame(frame, file.Position));
        }

        file.Write(new byte[trailing]);
        return file.ToArray();
    }

    public static byte[] Frame(Sl2Frame frame, long position)
    {
        var size = (ushort)(144 + frame.Echo.Length);
        var bytes = new byte[144 + frame.Echo.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), frame.OffsetField ?? (uint)position);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(28), frame.FrameSize ?? size);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(30), frame.PreviousFrameSize);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(32), frame.Channel);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(34), frame.EchoSize ?? (ushort)frame.Echo.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), frame.FrameIndex);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(40), frame.Upper);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(44), frame.Lower);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(64), frame.Depth);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(100), frame.Speed);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(108), frame.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(112), frame.Y);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(120), frame.Course);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(132), frame.Flags);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(140), frame.Time);
        frame.Echo.CopyTo(bytes, 144);
        return bytes;
    }
}
