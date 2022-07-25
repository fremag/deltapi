using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace deltapi_engine;

public class DeltApiEngine
{
    private IHttpClient ClientA { get; }
    private IHttpClient ClientB { get; }
    private IDateTimeService DateTimeService { get; }
    public event Action<int, DeltApiActionReport> ActionReportPublished;
    
    public DeltApiEngine(IHttpClient clientA, IHttpClient  clientB, IDateTimeService dateTimeService)
    {
        ClientA = clientA;
        ClientB = clientB;
        DateTimeService = dateTimeService;
    }

    public async Task<DeltApiReport> Run(List<DeltApiAction> actions)
    {
        var begin = DateTimeService.Now;
        var reports = new DeltApiActionReport[actions.Count];
        
        for (var i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            var actionReport = await Run(action);

            reports[i] = actionReport;
            ActionReportPublished?.Invoke(i, actionReport);
        }

        var end = DateTimeService.Now;
        var report = new DeltApiReport
        {
            Begin = begin,
            End = end,
            Reports = reports,
        };

        return report;
    }

    public async Task<DeltApiActionReport> Run(DeltApiAction action)
    {
        var resultA = await Run(ClientA, action);
        var resultB = await Run(ClientB, action);
        var actionReport = Compare(action, resultA, resultB);
        return actionReport;
    }

    private DeltApiActionReport Compare(DeltApiAction action, DeltApiActionResult resultA, DeltApiActionResult resultB)
    {
        ReportStatus status = resultA.StatusCode == resultB.StatusCode ? ReportStatus.Success : ReportStatus.Failure;
        
        if (status != ReportStatus.Failure)
        {
            var strA = JsonSerializer.Serialize(resultA.Content);
            var strB = JsonSerializer.Serialize(resultB.Content);
            status = strA == strB ? ReportStatus.Success : ReportStatus.Failure;
        }

        return new DeltApiActionReport
        {
            Action = action,
            ResultA = resultA,
            ResultB = resultB,
            Status = status
        };        
    }

    public async Task<DeltApiActionResult> Run(IHttpClient client, DeltApiAction action)
    {
        var stopWatch = Stopwatch.StartNew();
        var url = $"{action.Url}";
        dynamic content = null;
        HttpStatusCode? statusCode;

        try
        {
            HttpResponseMessage responseMessage = null;
            switch (action.Verb)
            {
                case Verbs.Get:
                    responseMessage = await client.GetAsync(url);
                    break;
                case Verbs.Put:
                    responseMessage = await client.PutAsync(url, GetContent(action));
                    break;
                case Verbs.Post:
                    responseMessage = await client.PostAsync(url, GetContent(action));
                    break;
                case Verbs.Delete:
                    responseMessage = await client.DeleteAsync(url);
                    break;
                case Verbs.Patch:
                    responseMessage = await client.PatchAsync(url, GetContent(action));
                    break;
            }

            statusCode = responseMessage?.StatusCode;
            var responseMessageContent = responseMessage?.Content;
            if (responseMessageContent != null)
            {
                content = await responseMessageContent.ReadFromJsonAsync<dynamic>() ?? string.Empty;
            }
        }
        catch (Exception e)
        {
            statusCode = null;
            content = JsonSerializer.Serialize(new {e.Message, e.StackTrace, e.GetType().Name});
        }
        return new DeltApiActionResult
        {
            Duration = stopWatch.Elapsed,
            StatusCode = statusCode,
            Content = content
        };
    }

    private HttpContent GetContent(DeltApiAction action) => new StringContent(action.Data ?? string.Empty);
}