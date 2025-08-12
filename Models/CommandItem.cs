namespace SSHExplorer.Models;

public class CommandItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty; // e.g. "ls -la {path}"
    public List<CommandParameter> Parameters { get; set; } = new();
}

public class CommandParameter
{
    public string Key { get; set; } = string.Empty; // {key}
    public string Label { get; set; } = string.Empty;
    public string? Default { get; set; }
    public bool Required { get; set; }
}
