using System;
using System.Threading.Tasks;
using TripleG3.SSH.WinUI.Models.Profiles;

namespace TripleG3.SSH.WinUI.Models.SSH
{
    public interface ISSHService
    {
        SSHState State { get; }

        event Action<SSHState> StateChanged;

        ValueTask ConnectAsync(Profile profile);
        ValueTask DisconnectAsync();
    }
}