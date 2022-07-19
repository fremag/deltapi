namespace deltapi_engine;

public interface IHttpClient
{
    Task<HttpResponseMessage> GetAsync(string requestUri);
    Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content);
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    Task<HttpResponseMessage> DeleteAsync(string requestUri);
    Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content);
}

public class BasicHttpClient : IHttpClient
{
    private HttpClient client;

    public BasicHttpClient(string baseAddress)
    {
        client = new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        };
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri) => client.GetAsync(requestUri);

    public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content) => client.PutAsync(requestUri, content);

    public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content) => client.PostAsync(requestUri, content);
    
    public Task<HttpResponseMessage> DeleteAsync(string requestUri) => client.DeleteAsync(requestUri);
    
    public Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content)=> client.PatchAsync(requestUri, content);
}