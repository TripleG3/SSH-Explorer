using System.Diagnostics;

namespace TripleG3.SSH.WinUI.Models.SSH;

public interface IProcessFactory
{
    IProcessWrapper Create(ProcessStartInfo startInfo, bool enableRaisingEvents = true);
}
