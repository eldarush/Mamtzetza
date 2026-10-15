using System.Text;

namespace Mamtzetza;

/// <summary>Duplicates real console output to a flushed file, without fabricating log entries.</summary>
public sealed class StdoutCapture : TextWriter
{
    private readonly TextWriter original;
    private readonly StreamWriter file;
    private readonly object gate = new();
    public override Encoding Encoding => original.Encoding;

    private StdoutCapture(string path)
    {
        original = Console.Out;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        file = new StreamWriter(path, append: false, new UTF8Encoding(false)) { AutoFlush = true };
        Console.SetOut(this);
    }

    public static StdoutCapture? Install(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : new StdoutCapture(path);

    public override void Write(char value) { lock (gate) { original.Write(value); file.Write(value); } }
    public override void Write(string? value) { lock (gate) { original.Write(value); file.Write(value); } }
    public override void WriteLine(string? value) { lock (gate) { original.WriteLine(value); file.WriteLine(value); } }
    public override void Flush() { lock (gate) { original.Flush(); file.Flush(); } }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { lock (gate) { Console.SetOut(original); file.Dispose(); } }
        base.Dispose(disposing);
    }
}
