using CommandLine;

namespace deltapi;

public class Options
{
    [Option('f', "files", Required = false, HelpText = "Input files")]
    public IEnumerable<string> Files { get; set; }
    
    [Option('a', "serverA", Required = true, HelpText = "server A")]
    public string ServerA { get; set; }
    [Option('b', "serverB", Required = true, HelpText = "server B")]
    public string ServerB { get; set; }
    
    [Option('r', "report", Required = false, HelpText = "Report file")]
    public string Report { get; set; }
}