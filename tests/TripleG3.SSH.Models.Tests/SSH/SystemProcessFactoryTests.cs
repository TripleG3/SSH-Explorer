using System.Diagnostics;
using System.Text;
using TripleG3.SSH.WinUI.Models.SSH;

namespace TripleG3.SSH.WinUI.Models.Tests.SSH;

[TestClass]
public sealed class SystemProcessFactoryTests
{
    [TestMethod]
    public async Task Create_StartsProcess_ReadsStdOut_ExitCodeZero()
    {
        var startInfo = new ProcessStartInfo("cmd.exe", "/c echo hello")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        var factory = new SystemProcessFactory();
        using IProcessWrapper proc = factory.Create(startInfo);

        proc.Start();
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync(CancellationToken.None);

        Assert.IsTrue(proc.HasExited);
        Assert.AreEqual(0, proc.ExitCode);
        Assert.AreEqual("hello", stdout.Trim());
    }

    [TestMethod]
    public void Create_Kill_LongRunningProcess_Exits()
    {
        // Use a ping to create a process that runs for a few seconds
        var startInfo = new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 10")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        var factory = new SystemProcessFactory();
        using IProcessWrapper proc = factory.Create(startInfo);

        proc.Start();
        // Give it a moment to ensure the process is running
        proc.WaitForExit(200); // ignore result; wrapper signature returns void

        // Kill the process and verify it exits promptly
        proc.Kill(entireProcessTree: true);
        proc.WaitForExit(5000);

        Assert.IsTrue(proc.HasExited, "Process did not exit after Kill().");
    }
}
