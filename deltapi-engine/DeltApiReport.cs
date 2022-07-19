namespace deltapi_engine;

public class DeltApiReport
{
    public DateTime Begin { get; set; }
    public DateTime End { get; set; }
    public DeltApiActionReport[] Reports { get; set; }
    public TimeSpan TotalTime => End - Begin;
}