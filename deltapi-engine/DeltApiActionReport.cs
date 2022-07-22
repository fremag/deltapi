using System.Diagnostics;

namespace deltapi_engine;

public enum ReportStatus {Success, Failure}
[DebuggerDisplay("{DebugString}")]
public class DeltApiActionReport
{
    public DeltApiAction Action { get; set; }
    public ReportStatus Status { get; set; }
    public DeltApiActionResult ResultA { get; set; }
    public DeltApiActionResult ResultB { get; set; }

    public string DebugString => $"[{Status}] {Action.Verb} - {Action.Url}";
}