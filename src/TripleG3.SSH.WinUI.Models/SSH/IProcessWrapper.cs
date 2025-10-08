using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

public interface IProcessWrapper : IDisposable
{
    event Action? Exited;
    StreamReader StandardOutput { get; }
    StreamReader StandardError { get; }
    StreamWriter StandardInput { get; }
    bool HasExited { get; }
    int ExitCode { get; }
    void Start();
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
    void WaitForExit(int milliseconds);
    void Kill(bool entireProcessTree);
}
