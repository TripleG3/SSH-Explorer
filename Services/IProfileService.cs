using SSHExplorer.Models;

namespace SSHExplorer.Services;

public interface IProfileService
{
    Task<IReadOnlyList<Profile>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<Profile> profiles, CancellationToken ct = default);
    Task AddOrUpdateAsync(Profile profile, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}
