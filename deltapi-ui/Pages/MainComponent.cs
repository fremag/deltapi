using deltapi_engine;
using GridBlazor;
using GridCore.Server;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Primitives;

namespace deltapi_ui.Pages;

public class MainComponent : ComponentBase
{
    [Inject]
    private IDateTimeService DateTimeService { get; set; }

    protected RunConfig RunConfigModel { get; } = new();

    protected class RunConfig
    {
        public string ServerA { get; set; } = "http://localhost:5000";
        public string ServerB { get; set; } = "http://localhost:6000";
    }
    
    protected CGrid<DeltApiAction> actionGrid;
    protected Task loadingTask;
    private List<DeltApiAction> Actions { get; }= new();

    private void GetColumns(IGridColumnCollection<DeltApiAction> columns)
    {
        columns.Add(a => a.Verb);
        columns.Add(a => a.Url);
    }
    
    protected override async Task OnParametersSetAsync()
    {
        var query = new QueryDictionary<StringValues> { { "grid-page", "1" } };
        var client = new GridClient<DeltApiAction>(GetActionRows, query, false, "actionsGrid", GetColumns);
        actionGrid = client.Grid;
        
        loadingTask = client.UpdateGrid();
        await loadingTask;
    }

    private ItemsDTO<DeltApiAction> GetActionRows(QueryDictionary<StringValues> queryDictionary)
    {
        var server = new GridCoreServer<DeltApiAction>(Actions, new QueryCollection(queryDictionary), true, "actionsGrid", GetColumns, 10);
        var rows = server.ItemsToDisplay;
        return rows;
    }

    protected async Task LoadFiles(InputFileChangeEventArgs e)
    {
        Actions.Clear();
        foreach (var file in e.GetMultipleFiles())
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var actions = DeltApiActionReader.ReadActions(reader);
            await foreach (var action in actions)
            {
                Actions.Add(action);
            }
        }

        await actionGrid.UpdateGrid();
    }
    
    protected async Task RunEngine()
    {
        var clientA = new BasicHttpClient(RunConfigModel.ServerA);
        var clientB = new BasicHttpClient(RunConfigModel.ServerB);

        var engine = new DeltApiEngine(clientA, clientB, Actions, DateTimeService);
        var report = await engine.Run();
    }
}

