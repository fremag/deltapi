namespace deltapi_engine;

public interface IDeltApiActionReader
{
    IAsyncEnumerable<DeltApiAction> ReadActions(StreamReader reader);
}