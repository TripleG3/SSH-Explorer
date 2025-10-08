using System.Text.Json;
using TripleG3.SSH.WinUI.Models.Profiles;

namespace TripleG3.SSH.WinUI.Models.Tests.Profiles;

[TestClass]
public sealed class ProfilesServiceTests
{
    private sealed class FakeProfileStorage : IProfileStorage
    {
        private readonly Dictionary<string, string> files = new(StringComparer.Ordinal);

        public ValueTask<bool> ExistsAsync(string fileName) => ValueTask.FromResult(files.ContainsKey(fileName));

        public ValueTask CreateOrReplaceAsync(string fileName, string content)
        {
            files[fileName] = content;
            return ValueTask.CompletedTask;
        }

        public ValueTask<string> ReadAsync(string fileName)
        {
            if (!files.TryGetValue(fileName, out var content)) throw new InvalidOperationException("File not found");
            return ValueTask.FromResult(content);
        }

        public ValueTask DeleteAsync(string fileName)
        {
            files.Remove(fileName);
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<string>> EnumerateAsync(string extension)
        {
            var list = files.Keys.Where(k => k.EndsWith(extension, StringComparison.OrdinalIgnoreCase)).ToList();
            return ValueTask.FromResult<IReadOnlyList<string>>(list);
        }
    }

    private static Profile NewProfile(string name) => new(name, "127.0.0.1", "user", "pw", ProfileConstants.DefaultPort);

    [TestMethod]
    public async Task LoadProfiles_LoadsCreatedProfiles_AndTogglesBusy()
    {
        var storage = new FakeProfileStorage();
        // Seed files
        var p1 = NewProfile("p1");
        var p2 = NewProfile("p2");
        await storage.CreateOrReplaceAsync($"{p1.Name}.{ProfileConstants.ProfileExtension}", JsonSerializer.Serialize(p1));
        await storage.CreateOrReplaceAsync($"{p2.Name}.{ProfileConstants.ProfileExtension}", JsonSerializer.Serialize(p2));
        await storage.CreateOrReplaceAsync("random.txt", "not a profile");

        var svc = new ProfilesService(storage);
        var events = new List<ProfilesState>();
        svc.StateChanged += s => events.Add(s);

        await svc.LoadProfiles();

        Assert.HasCount(2, events);
        Assert.IsTrue(events[0].IsBusy);
        Assert.IsFalse(events[^1].IsBusy);

        var names = svc.State.Profiles.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(p1.Name, names);
        Assert.Contains(p2.Name, names);
    }

    [TestMethod]
    public async Task DeleteAllProfiles_DeletesOnlyProfileFiles_AndClearsState()
    {
        var storage = new FakeProfileStorage();
        var p1 = NewProfile("p1");
        var p2 = NewProfile("p2");
        await storage.CreateOrReplaceAsync($"{p1.Name}.{ProfileConstants.ProfileExtension}", JsonSerializer.Serialize(p1));
        await storage.CreateOrReplaceAsync($"{p2.Name}.{ProfileConstants.ProfileExtension}", JsonSerializer.Serialize(p2));
        await storage.CreateOrReplaceAsync("keep.txt", "keep");

        var svc = new ProfilesService(storage);
        var events = new List<ProfilesState>();
        svc.StateChanged += s => events.Add(s);

        await svc.DeleteAllProfiles();

        Assert.HasCount(2, events);
        Assert.IsTrue(events[0].IsBusy);
        Assert.IsFalse(events[^1].IsBusy);
        Assert.IsEmpty(svc.State.Profiles);

        // Ensure .sshprofile files are gone, keep.txt remains
        var remaining = await storage.EnumerateAsync($".{ProfileConstants.ProfileExtension}");
        Assert.IsEmpty(remaining);
        // Since EnumerateAsync filters by extension, we check that non-profile isn't affected by attempting to read it
        var nonProfileContent = await storage.ReadAsync("keep.txt");
        Assert.AreEqual("keep", nonProfileContent);
    }
}
