using System;
using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.SSH;

[ExcludeFromCodeCoverage(Justification = "Data model")]
public sealed record SshTimelineEntry(Guid SessionId, DateTimeOffset Timestamp, SshTimelineKind Kind, Guid? CommandId, string Text);
