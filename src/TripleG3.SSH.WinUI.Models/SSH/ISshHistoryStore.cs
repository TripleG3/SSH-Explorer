using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

public interface ISshHistoryStore
{
    Task SaveAsync(SshSessionHistory session, CancellationToken ct = default);
    Task<IReadOnlyList<SshSessionHistory>> LoadAsync(Profiles.Profile profile, CancellationToken ct = default);
}
