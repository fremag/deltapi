using System.Text.Json;
using deltapi_engine;
using deltapi_utils;
using GridBlazor;
using GridBlazor.Pages;
using GridCore.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Primitives;
using NLog;

namespace deltapi_ui.Pages;

public class MainComponent : ComponentBase
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public enum Status {Running, Paused, Ready}
    
    [Inject]
    private IDateTimeService DateTimeService { get; set; }

    [Inject]
    IDeltApiActionReader DeltApiActionReader { get; set; }
    
    protected RunConfig RunConfigModel { get; } = new();
    protected MarkupString ContentA { get; private set; }
    protected MarkupString ContentB { get; private set; }
    protected string StatusA { get; private set; }
    protected string StatusB { get; private set; }

    protected class RunConfig
    {
        public string ServerA { get; set; } = "http://localhost:5000";
        public string ServerB { get; set; } = "http://localhost:6000";
    }
    
    protected CGrid<DeltApiActionReport> reportGrid;
    protected GridComponent<DeltApiActionReport> reportGridComponent;
    
    protected Task loadingTask;
    private List<DeltApiActionReport> Reports { get; } = new();
    public Status CurrentStatus { get; set; } = Status.Ready; 
    
    protected override async Task OnParametersSetAsync()
    {
        var reportQuery = new QueryDictionary<StringValues> { { "grid-page", "1" } };
        var reportClient = new GridClient<DeltApiActionReport>(GetReportRows, reportQuery, false, "reportGrid", GetReportColumns);
        reportGrid = reportClient.Grid;
        reportClient.Selectable(true);
        reportGrid.SetRowCssClassesContraint(report =>
        {
            switch (report.Status)
            {
                case ReportStatus.Waiting:
                    return "alert-primary";
                case ReportStatus.Running:
                    return "alert-warning";
                case ReportStatus.Success:
                    return "alert-success";
                case ReportStatus.Failure:
                    return "alert-danger";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
        loadingTask = reportGrid.UpdateGrid();
        
        loadingTask = reportClient.UpdateGrid();
        await loadingTask;
    }

    private void GetReportColumns(IGridColumnCollection<DeltApiActionReport> columns)
    {
        columns.Add(a => a.Status) 
            .Encoded(false)
            .Sanitized(false)
            .RenderValueAs(report => Icon(report.Status switch
        {
            ReportStatus.Failure => "circle-x",
            ReportStatus.Running => "play-circle",
            ReportStatus.Success => "circle-check",
            ReportStatus.Waiting => "clock",
            _ => throw new ArgumentOutOfRangeException()
        }).Value)
            .Titled("")
            .SetTooltip("Status");
        
        columns.Add(a => a.Action.Verb).Titled("Verb");
        columns.Add(a => a.Action.Url).Titled("Url");
        columns.Add(a => a.ResultA.Duration.TotalMilliseconds).Titled("A (ms)").Format("{0:###,##0.00}").SetCellCssClassesContraint(_ => "number");
        columns.Add(a => a.ResultB.Duration.TotalMilliseconds).Titled("B (ms)").Format("{0:###,##0.00}").SetCellCssClassesContraint(_ => "number");
    }

    protected static MarkupString Icon(string name)
    {
        return (MarkupString)$"<span class=\"oi oi-{name}\"/>";
    }

    private ItemsDTO<DeltApiActionReport> GetReportRows(QueryDictionary<StringValues> queryDictionary)
    {
        var server = new GridCoreServer<DeltApiActionReport>(Reports, new QueryCollection(queryDictionary), true, "reportsGrid", GetReportColumns, 10);
        var rows = server.ItemsToDisplay;
        return rows;
    }

    protected async Task LoadFiles(InputFileChangeEventArgs e)
    {
        Reports.Clear();
        foreach (var file in e.GetMultipleFiles())
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var actions = DeltApiActionReader.ReadActions(reader);
            await foreach (var action in actions)
            {
                Reports.Add(new DeltApiActionReport
                {
                    Action = action,
                    Status = ReportStatus.Waiting,
                    ResultA = new DeltApiActionResult{Content = null, Duration = TimeSpan.Zero, StatusCode = null},
                    ResultB = new DeltApiActionResult{Content = null, Duration = TimeSpan.Zero, StatusCode = null}
                });
            }
        }
        await reportGridComponent.UpdateGrid();
    }

    protected void PauseEngine()
    {
        Logger.ExtInfo("Pause", new {CurrentStatus});
        CurrentStatus = Status.Paused;
    }
    
    protected void StopEngine()
    {
        Logger.ExtInfo("Stop", new {CurrentStatus});
        CurrentStatus = Status.Ready;
    }
    
    protected async Task RunEngine()
    {
        if (CurrentStatus == Status.Paused)
        {
            Logger.ExtInfo("Run", new {CurrentStatus});
            CurrentStatus = Status.Running;
            return;
        }
        CurrentStatus = Status.Running;
        
        var clientA = new BasicHttpClient(RunConfigModel.ServerA);
        var clientB = new BasicHttpClient(RunConfigModel.ServerB);

        var engine = new DeltApiEngine(clientA, clientB, DateTimeService);
        foreach (var actionReport in Reports)
        {
            actionReport.Status = ReportStatus.Waiting;
        }
        await reportGridComponent.UpdateGrid();

        for (int i = 0; i < Reports.Count; i++)
        {
            var actionReport = Reports[i];
            while (CurrentStatus == Status.Paused )
            {
                Logger.ExtInfo("Pause...", new {CurrentStatus , Progress = $"{i+1} / {Reports.Count}" });
                await Task.Delay(1_000);
            }

            if (CurrentStatus == Status.Ready)
            {
                Logger.ExtInfo("Stop...", new {CurrentStatus , Progress = $"{i+1} / {Reports.Count}" });
                return;
            }
            
            Logger.ExtInfo("Run...", new {CurrentStatus , Progress = $"{i+1} / {Reports.Count}" });
            actionReport.Status = ReportStatus.Running;
            await reportGridComponent.UpdateGrid();
            await Task.Delay(100);
            var action = actionReport.Action;
            var report = await engine.Run(action);
            Reports[i] = report;
            await reportGridComponent.UpdateGrid();
            await Task.Delay(1_000);
        }
    }
    
    protected void OnRowClicked(object obj)
    {
        if (obj is DeltApiActionReport report)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            ContentA = ReferenceEquals(null, report.ResultA.Content) ? Icon("ban") : (MarkupString)JsonSerializer.Serialize(report.ResultA.Content, options);
            ContentB = ReferenceEquals(null, report.ResultB.Content) ? Icon("ban") : (MarkupString)JsonSerializer.Serialize(report.ResultB.Content, options);
            
            StatusA = report.ResultA.StatusCode?.ToString();
            StatusB = report.ResultB.StatusCode?.ToString();
        }
        StateHasChanged();
    }
}


