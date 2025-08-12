using System.Text.Json;
using SSHExplorer.Models;

namespace SSHExplorer.Services;

public sealed class ProfileService : IProfileService
{
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ProfileService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "TripleG3", "SSHExplorer");
        Directory.CreateDirectory(dir);
        _storePath = Path.Combine(dir, "profiles.json");
    }

    public async Task<IReadOnlyList<Profile>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_storePath)) return Array.Empty<Profile>();
        await using var fs = File.OpenRead(_storePath);
        var profiles = await JsonSerializer.DeserializeAsync<List<Profile>>(fs, cancellationToken: ct) ?? new();
        return profiles;
    }

    public async Task SaveAsync(IEnumerable<Profile> profiles, CancellationToken ct = default)
    {
        await using var fs = File.Create(_storePath);
        await JsonSerializer.SerializeAsync(fs, profiles, _jsonOptions, ct);
    }

    public async Task AddOrUpdateAsync(Profile profile, CancellationToken ct = default)
    {
        var profiles = (await LoadAsync(ct)).ToList();
        var existing = profiles.FindIndex(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0) profiles[existing] = profile; else profiles.Add(profile);
        await SaveAsync(profiles, ct);
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        var profiles = (await LoadAsync(ct)).Where(p => !string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        await SaveAsync(profiles, ct);
    }
}
