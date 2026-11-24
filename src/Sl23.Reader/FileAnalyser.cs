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
    public static FileReport Analyse(string path, Action<long>? progress = null) => throw new NotImplementedException();

    /// <summary>Analyses an open file.</summary>
    /// <param name="stream">The file; it must be readable and seekable.</param>
    /// <param name="path">The path to carry in the result.</param>
    /// <param name="progress">Called with the file position now and then while frames are read; it may be null.</param>
    /// <returns>The file's result.</returns>
    public static FileReport Analyse(Stream stream, string path, Action<long>? progress = null) => throw new NotImplementedException();
}
