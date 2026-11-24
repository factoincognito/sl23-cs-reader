namespace Sl23.Reader;

/// <summary>Reads one SL2 or SL3 file through to its inventory (design sections 4 and 7.2).</summary>
public static class FileAnalyser
{
    /// <summary>The bytes the reading must advance before the progress callback is called again.</summary>
    public const long ProgressStepBytes = 1L << 20;

    /// <summary>Opens a file and analyses it.</summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="progress">Called with the file position now and then while frames are read; it may be null.</param>
    /// <returns>The file's result; a file that cannot be read has a <see cref="FileProblem"/> and no inventory.</returns>
    public static FileReport Analyse(string path, Action<long>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan);
        }
        catch (Exception exception) when (Classify(exception) is { } problem)
        {
            return new FileReport(path, null, problem);
        }

        using (stream)
        {
            return Analyse(stream, path, progress);
        }
    }

    /// <summary>Analyses an open file.</summary>
    /// <param name="stream">The file; it must be readable and seekable.</param>
    /// <param name="path">The path to carry in the result.</param>
    /// <param name="progress">Called with the file position now and then while frames are read; it may be null.</param>
    /// <returns>The file's result.</returns>
    public static FileReport Analyse(Stream stream, string path, Action<long>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(path);

        long? size = null;
        try
        {
            size = stream.Length;
            stream.Position = 0;
            var bytes = new byte[FileHeader.Size];
            var read = ReadUpTo(stream, bytes);
            if (!FileHeader.TryParse(bytes.AsSpan(0, read), out var header))
            {
                return new FileReport(path, null, FileProblem.TooShortForHeader, size);
            }

            if (!header.HasReadableFrames)
            {
                return new FileReport(path, null, FileProblem.FormatNotRead, size, header);
            }

            return new FileReport(path, Inventory(stream, header, size.Value, progress), FileProblem.None, size, header);
        }
        catch (Exception exception) when (Classify(exception) is { } problem)
        {
            return new FileReport(path, null, problem == FileProblem.NotFound ? FileProblem.ReadFailed : problem, size);
        }
    }

    private static FileInventory Inventory(Stream stream, FileHeader header, long size, Action<long>? progress)
    {
        var builder = new InventoryBuilder(header, size);
        Func<FrameRecord?> next;
        Func<ChainBreak?> chainBreak;
        if (header.Format == FileFormat.Sl2)
        {
            var reader = new Sl2FrameReader(stream);
            next = reader.ReadNext;
            chainBreak = () => reader.Break;
        }
        else
        {
            var reader = new Sl3FrameReader(stream);
            next = reader.ReadNext;
            chainBreak = () => reader.Break;
        }

        var reported = 0L;
        while (next() is { } frame)
        {
            builder.Add(frame);
            if (progress is not null && stream.Position - reported >= ProgressStepBytes)
            {
                reported = stream.Position;
                progress(reported);
            }
        }

        return builder.Complete(chainBreak());
    }

    private static int ReadUpTo(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private static FileProblem? Classify(Exception exception) => exception switch
    {
        FileNotFoundException or DirectoryNotFoundException or PathTooLongException or ArgumentException or NotSupportedException => FileProblem.NotFound,
        UnauthorizedAccessException => FileProblem.AccessDenied,
        IOException => FileProblem.ReadFailed,
        _ => null,
    };
}
