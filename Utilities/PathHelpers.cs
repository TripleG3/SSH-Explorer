namespace SSHExplorer.Utilities;

public static class PathHelpers
{
    public static string CombineUnix(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b;
        if (a.EndsWith('/')) return a + b;
        return a + "/" + b;
    }

    public static string ParentUnix(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return "/";
        var idx = path.TrimEnd('/').LastIndexOf('/');
        return idx <= 0 ? "/" : path[..idx];
    }
}
