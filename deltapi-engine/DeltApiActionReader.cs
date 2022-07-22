using System.IO.Abstractions;
using deltapi_utils;
using NLog;

namespace deltapi_engine;

public class DeltApiActionReader
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public DeltApiActionReader(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    private IFileSystem FileSystem { get; set; }
    
    public List<DeltApiAction> ReadActions(string path)
    {
        if (! FileSystem.File.Exists(path))
        {
            Logger.Error($"{nameof(ReadActions)}: file not found ! {path}");
            throw new FileNotFoundException(path);
        }

        Logger.ExtInfo("Reading...", new {path});
        var lines = FileSystem.File.ReadAllLines(path);
        var deltApiActions = ReadActions(lines).ToList();
        Logger.ExtInfo("Read.", new {deltApiActions.Count, path});

        return deltApiActions;
    }
    
    public static  IEnumerable<DeltApiAction> ReadActions(IEnumerable<string> lines) => lines.Where(line => ! line.StartsWith("#") && ! string.IsNullOrEmpty(line.Trim())).Select(Parse);

    public static async IAsyncEnumerable<DeltApiAction> ReadActions(StreamReader reader)
    {
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("#"))
            {
                continue;
            }

            line = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            yield return Parse(line);
        }
    } 

    public static DeltApiAction Parse(string line)
    {
        var items = line.Split(',');
        if (!Enum.TryParse<Verbs>(items[0], ignoreCase: true, out var verb))
        {
            Logger.Error($"{nameof(Parse)}: unknown verb ! {new {Verb=items[0], line}}");
        }

        var url = items[1];
        string data = null;
        if (items.Length > 2)
        {
            data = string.Join(",", items.Skip(2));
        }
        
        return new DeltApiAction
        {
            Verb = verb,
            Url = url,
            Data = data
        };
    }
}