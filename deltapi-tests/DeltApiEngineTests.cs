using System.Net;
using System.Text.Json;
using deltapi_engine;
using NFluent;
using NSubstitute;

namespace deltapi_tests;

public class DeltApiEngineTests
{
    private readonly IHttpClient clientA = Substitute.For<IHttpClient>();
    private readonly IHttpClient clientB = Substitute.For<IHttpClient>();
    private readonly IDateTimeService dateTimeService = Substitute.For<IDateTimeService>();
    private DeltApiEngine engine;
    private const string Meth1 = "/api/Meth1";
    private List<DeltApiAction> actions;

    [SetUp]
    public void Setup()
    {
        actions = new List<DeltApiAction>();
        engine = new DeltApiEngine(clientA, clientB, dateTimeService);
    }

    [Test]
    public async Task Get_Failure_Test()
    {
        SetUp(Verbs.Get, clientA, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true });
        SetUp(Verbs.Get, clientB, Meth1, HttpStatusCode.OK, new { Name = "BBBB", Id = 2, Enabled = false });
        actions.Add(new DeltApiAction { Verb = Verbs.Get, Url = Meth1 });

        DeltApiReport report = await engine.Run(actions);
        Check.That(report.Reports).CountIs(1);
        Check.That(report.Reports[0].Action).IsEqualTo(actions[0]);
        Check.That(report.Reports[0].Status).IsEqualTo(ReportStatus.Failure);
    }

    [Test]
    public async Task Get_Success_Test()
    {
        SetUp(Verbs.Get, clientA, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true });
        SetUp(Verbs.Get, clientB, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true });
        actions.Add(new DeltApiAction { Verb = Verbs.Get, Url = Meth1 });

        DeltApiReport report = await engine.Run(actions);
        Check.That(report.Reports).CountIs(1);
        Check.That(report.Reports[0].Action).IsEqualTo(actions[0]);
        Check.That(report.Reports[0].Status).IsEqualTo(ReportStatus.Success);
    }

    [Test]
    public async Task Post_Failure_Test()
    {
        var data = new { Id = 1 };
        actions.Add(new() { Verb = Verbs.Post, Url = Meth1, Data = JsonSerializer.Serialize(data)});
        SetUp(Verbs.Post, clientA, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true }, data);
        SetUp(Verbs.Post, clientB, Meth1, HttpStatusCode.OK, new { Name = "BBBB", Id = 2, Enabled = false }, data);
        DeltApiReport report = await engine.Run(actions);
        Check.That(report.Reports).CountIs(1);
        Check.That(report.Reports[0].Action).IsEqualTo(actions[0]);
        Check.That(report.Reports[0].Status).IsEqualTo(ReportStatus.Failure);
    }

    [Test]
    public async Task Post_Success_Test()
    {
        var data = new { Id = 1 };
        actions.Add(new() { Verb = Verbs.Post, Url = Meth1, Data = JsonSerializer.Serialize(data)});
        SetUp(Verbs.Post, clientA, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true }, data);
        SetUp(Verbs.Post, clientB, Meth1, HttpStatusCode.OK, new { Name = "AAAA", Id = 1, Enabled = true }, data);
        DeltApiReport report = await engine.Run(actions);
        Check.That(report.Reports).CountIs(1);
        Check.That(report.Reports[0].Action).IsEqualTo(actions[0]);
        Check.That(report.Reports[0].Status).IsEqualTo(ReportStatus.Success);
    }

    private static void SetUp(Verbs verb, IHttpClient httpClient, string url, HttpStatusCode code, dynamic content, dynamic data = null)
    {
        var contentStr = JsonSerializer.Serialize(content);
        HttpResponseMessage res = new HttpResponseMessage(code) { Content = new StringContent(contentStr) };
        Task<HttpResponseMessage> resp = Task.FromResult(res);
        switch (verb)
        {
            case Verbs.Get:
                httpClient.GetAsync(url).Returns(resp);
                break;
            case Verbs.Post:
                string dataStr = JsonSerializer.Serialize(data);
                var httpContentArg = Arg.Is<HttpContent>(httpContent => CheckContent(httpContent, dataStr));
                httpClient.PostAsync(url, httpContentArg).Returns(resp);
                break;
        }
    }

    private static bool CheckContent(HttpContent httpContent, string dataStr)
    {
        var stream = httpContent.ReadAsStream();
        using var reader = new StreamReader(stream);
        var value = reader.ReadToEnd();
        var result = value == dataStr;
        return result;
    }
}