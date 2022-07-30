using System.IO.Abstractions;
using deltapi_utils;

namespace deltapi_engine;

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