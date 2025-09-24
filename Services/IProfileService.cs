using SSHExplorer.Models;

namespace SSHExplorer.Services;

public interface IProfileService : IStatePublisher<ProfileState>
{
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<Profile> profiles, CancellationToken ct = default);
    Task AddOrUpdateAsync(Profile profile, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
    Task SelectProfileAsync(Profile? profile, CancellationToken ct = default);
}
