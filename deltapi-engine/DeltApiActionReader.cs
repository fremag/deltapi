using System.IO.Abstractions;
using deltapi_utils;
using NLog;

namespace deltapi_engine;

public class DeltApiActionReader : IDeltApiActionReader
{
    protected static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    public IEnumerable<DeltApiAction> ReadActions(IEnumerable<string> lines) => lines.Where(line => ! line.StartsWith("#") && ! string.IsNullOrEmpty(line.Trim())).Select(Parse);

    public async IAsyncEnumerable<DeltApiAction> ReadActions(StreamReader reader)
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

    public DeltApiAction Parse(string line)
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

public class DeltApiActionFileReader : DeltApiActionReader
{
    private IFileSystem FileSystem { get; set; }

    public DeltApiActionFileReader(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

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

}