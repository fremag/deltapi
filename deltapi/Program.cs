﻿// See https://aka.ms/new-console-template for more information

using System.IO.Abstractions;
using System.Text.Json;
using CommandLine;
using deltapi_engine;
using deltapi_utils;
using NLog;

namespace deltapi;

public static class Program
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private static Logger StatusLogger { get; } = LogManager.GetLogger("STATUS");

    public static async Task Main(string[] args)
    {
        Logger.ExtInfo("Start...");
        await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);
        Logger.Info("End.");
    }

    private static async Task Run(Options options)
    {
        var fileSystem = new FileSystem();
        DeltApiActionFileReader reader = new DeltApiActionFileReader(fileSystem);
        List<DeltApiAction> actions = new List<DeltApiAction>();
        foreach (var file in options.Files)
        {
            actions.AddRange(reader.ReadActions(file));
        }
        
        var clientA = new BasicHttpClient(options.ServerA);
        var clientB = new BasicHttpClient(options.ServerB);
        var engine = new DeltApiEngine(clientA, clientB, new DateTimeService());
        engine.ActionReportPublished += OnActionReportPublished;
        var report = await engine.Run(actions);
        var nbSuccess = report.Reports.Count(actionReport => actionReport.Status == ReportStatus.Success);
        var nbFailure = report.Reports.Count(actionReport => actionReport.Status == ReportStatus.Failure);

        if (!string.IsNullOrEmpty(options.Report))
        {
            Logger.ExtInfo("Writing report...", new {File=options.Report});
            var json = JsonSerializer.Serialize(report);
            await fileSystem.File.WriteAllTextAsync(options.Report, json);
        }
        
        Logger.ExtInfo(new {nbSuccess, nbFailure, report.TotalTime});
    }

    private static void OnActionReportPublished(int i, DeltApiActionReport actionReport)
    {
        StatusLogger.Info($"#{i}\t[{actionReport.Status}]\t{actionReport.Action.Verb,-7}\t{actionReport.Action.Url}");
    }
}