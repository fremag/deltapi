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
        public int DelayMs { get; set; } = 100;
        public bool PauseAfterError { get; set; } = true;
    }
    
    protected CGrid<DeltApiActionReport> reportGrid;
    protected GridComponent<DeltApiActionReport> reportGridComponent;
    
    protected Task loadingTask;
    private BasicHttpClient ClientA { get; set; }
    private BasicHttpClient ClientB { get; set; }
    private DeltApiEngine Engine { get; set; }
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
            .RenderValueAs(report => Helper.Icon(report.Status switch
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
        columns.Add().RenderComponentAs<RunActionButton>(new List<Action<object>> { RunAction });
        columns.Add(a => a.ResultA.Duration.TotalMilliseconds).Titled("A (ms)").Format("{0:###,##0.00}").SetCellCssClassesContraint(_ => "number");
        columns.Add(a => a.ResultB.Duration.TotalMilliseconds).Titled("B (ms)").Format("{0:###,##0.00}").SetCellCssClassesContraint(_ => "number");
    }

    private async void RunAction(object obj)
    {
        if (obj is not DeltApiActionReport deltApiActionReport)
        {
            return;
        }

        if (Engine == null)
        {
            Init();
        }

        await Run(deltApiActionReport);
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

        Init();
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

            await Run(actionReport);
            
            if (RunConfigModel.PauseAfterError && actionReport.Status == ReportStatus.Failure )
            {
                Logger.ExtInfo("Report Status", new {CurrentStatus , Progress = $"{i+1} / {Reports.Count}", ReportStatus=actionReport.Status });
                CurrentStatus = Status.Paused;
            }
            
            await Task.Delay(RunConfigModel.DelayMs);
        }
    }

    private async Task Run(DeltApiActionReport deltApiActionReport)
    {
        var report = await Engine.Run(deltApiActionReport.Action);
        deltApiActionReport.Update(report);
        await reportGridComponent.UpdateGrid();
        DisplayResults(deltApiActionReport);
    }

    private void Init()
    {
        ClientA = new BasicHttpClient(RunConfigModel.ServerA);
        ClientB = new BasicHttpClient(RunConfigModel.ServerB);
        Engine = new DeltApiEngine(ClientA, ClientB, DateTimeService);
    }

    protected void OnRowClicked(object obj)
    {
        if (obj is not DeltApiActionReport report)
        {
            return;
        }

        DisplayResults(report);
    }

    private void DisplayResults(DeltApiActionReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        ContentA = ReferenceEquals(null, report.ResultA.Content) ? Helper.Icon("ban") : (MarkupString)JsonSerializer.Serialize(report.ResultA.Content, options);
        ContentB = ReferenceEquals(null, report.ResultB.Content) ? Helper.Icon("ban") : (MarkupString)JsonSerializer.Serialize(report.ResultB.Content, options);
            
        StatusA = report.ResultA.StatusCode?.ToString();
        StatusB = report.ResultB.StatusCode?.ToString();

        StateHasChanged();
    }
}


