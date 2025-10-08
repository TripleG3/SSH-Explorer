using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

internal sealed partial class SystemProcessWrapper : IProcessWrapper
{
    private readonly Process _proc;

    public event Action? Exited;

    public SystemProcessWrapper(Process proc)
    {
        _proc = proc;
        _proc.EnableRaisingEvents = true;
        _proc.Exited += (_, _) => Exited?.Invoke();
    }

    public StreamReader StandardOutput => _proc.StandardOutput;
    public StreamReader StandardError => _proc.StandardError;
    public StreamWriter StandardInput => _proc.StandardInput;
    public bool HasExited => _proc.HasExited;
    public int ExitCode => _proc.ExitCode;

    public void Start()
    {
        if (!_proc.Start())
            throw new InvalidOperationException("Failed to start the process.");
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) => _proc.WaitForExitAsync(cancellationToken);

    public void WaitForExit(int milliseconds) => _proc.WaitForExit(milliseconds);

    public void Kill(bool entireProcessTree) => _proc.Kill(entireProcessTree);

    public void Dispose() => _proc.Dispose();
}
