namespace Tendril.Worker.Infrastructure;

/// <summary>
/// Cross-platform single-instance guard using an exclusive file lock. The worker's startup
/// recovery assumes any task left in Running is an orphan, which is only true with one process.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly FileStream _stream;
    private SingleInstanceGuard(FileStream stream) => _stream = stream;

    public static SingleInstanceGuard? TryAcquire(string lockFilePath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(lockFilePath))!);

            var stream = new FileStream(lockFilePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);

            return new SingleInstanceGuard(stream);
        }
        catch (IOException)
        {
            return null; // someone else holds the lock
        }
    }

    public void Dispose() => _stream.Dispose(); // the OS also releases the lock if we crash
}
