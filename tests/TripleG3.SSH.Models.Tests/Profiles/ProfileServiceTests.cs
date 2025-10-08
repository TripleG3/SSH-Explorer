using TripleG3.SSH.WinUI.Models.Profiles;

namespace TripleG3.SSH.WinUI.Models.Tests.Profiles;

[TestClass]
public sealed class ProfileServiceTests
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
            // extension may be like ".sshprofile"; match file names that end with that value (case-insensitive)
            var list = files.Keys
                .Where(k => k.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return ValueTask.FromResult<IReadOnlyList<string>>(list);
        }
    }

    private static Profile Sample(string name = "p1") => new(name, "127.0.0.1", "u", "pw", ProfileConstants.DefaultPort);

    [TestMethod]
    public async Task CreateProfile_SavesAndSetsBusyState()
    {
        var storage = new FakeProfileStorage();
        var svc = new ProfileService(storage);
        var events = new List<ProfileState>();
        svc.StateChanged += s => events.Add(s);

        await svc.CreateProfile(Sample());

        Assert.IsFalse(svc.State.IsBusy);
        Assert.HasCount(2, events); // busy true then false
        Assert.IsTrue(events[0].IsBusy);
        Assert.IsFalse(events[1].IsBusy);
        Assert.IsTrue(await storage.ExistsAsync($"p1.{ProfileConstants.ProfileExtension}"));
    }

    [TestMethod]
    public async Task CreateProfile_EmptyName_Throws()
    {
        var svc = new ProfileService(new FakeProfileStorage());
        try
        {
            await svc.CreateProfile(Sample(name: " "));
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task CreateProfile_Duplicate_Throws()
    {
        var storage = new FakeProfileStorage();
        var svc = new ProfileService(storage);
        await svc.CreateProfile(Sample());
        try
        {
            await svc.CreateProfile(Sample());
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task LoadProfile_SetsStateProfile()
    {
        var storage = new FakeProfileStorage();
        var svc = new ProfileService(storage);
        var p = Sample();
        await svc.CreateProfile(p);

        await svc.LoadProfile(p.Name);

        Assert.AreEqual(p, svc.State.Profile);
        Assert.IsFalse(svc.State.IsBusy);
    }

    [TestMethod]
    public async Task LoadProfile_NotFound_Throws()
    {
        var svc = new ProfileService(new FakeProfileStorage());
        try
        {
            await svc.LoadProfile("missing");
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task DeleteProfile_RemovesFile_AndClearsSelectedIfSame()
    {
        var storage = new FakeProfileStorage();
        var svc = new ProfileService(storage);
        var p = Sample();
        await svc.CreateProfile(p);
        await svc.LoadProfile(p.Name);

        await svc.DeleteProfile(p.Name);

        Assert.IsFalse(await storage.ExistsAsync($"{p.Name}.{ProfileConstants.ProfileExtension}"));
        Assert.AreEqual(Profile.Empty, svc.State.Profile);
    }

    [TestMethod]
    public async Task UpdateProfile_WritesFile_AndUpdatesSelectedIfSame()
    {
        var storage = new FakeProfileStorage();
        var svc = new ProfileService(storage);
        var p = Sample();
        await svc.CreateProfile(p);
        await svc.LoadProfile(p.Name);

        var updated = p with { Address = "192.168.0.2" };
        await svc.UpdateProfile(updated);

        Assert.AreEqual(updated, svc.State.Profile);
    }
}

internal static class TestTaskExtensions
{
    public static async Task AsValueTask(this ValueTask task) => await task;
    public static async Task<T> AsValueTask<T>(this ValueTask<T> task) => await task;
}
