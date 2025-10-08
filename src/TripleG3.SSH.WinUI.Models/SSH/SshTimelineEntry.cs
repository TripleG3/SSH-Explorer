using System;

namespace TripleG3.SSH.WinUI.Models.SSH;

public sealed record SshTimelineEntry(Guid SessionId, DateTimeOffset Timestamp, SshTimelineKind Kind, Guid? CommandId, string Text);
