using System.Diagnostics;

namespace TripleG3.SSH.WinUI.Models.SSH;

public sealed class SystemProcessFactory : IProcessFactory
{
    public IProcessWrapper Create(ProcessStartInfo startInfo, bool enableRaisingEvents = true)
    {
        var p = new Process { StartInfo = startInfo, EnableRaisingEvents = enableRaisingEvents };
        return new SystemProcessWrapper(p);
    }
}
