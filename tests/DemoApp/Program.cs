using System.Text;
using SSC;
using SSC.Api.Handler;
using SSC.DB;
using SSC.DB.Attributes;
using SSC.DB.Hooks;
using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace DemoApp;

internal class TestHook : IHook<HttpServerExRequestContext>
{
    private static readonly byte[] l_Data = Encoding.UTF8.GetBytes("toto");
    public bool Intercept(HttpServerExRequestContext context)
    {
        context.ListenerResponse.AddHeader("toto", "toto");
        //context.ServerResponse.OutputStream.Write(l_Data, 0, l_Data.Length);
        return false;
    }
}

[DbTable(dbInstanceName: "Main", tableName: "toto")]
internal class TestModel : DbModel<TestModel>
{
    [DbField(primaryKey: true)]
    public string name;
    [DbField]
    public int age;
}

internal static class Program
{

    private static void Main()
    {
        Config.Database.Instance.Warmup();
        var cfg = Config.Database.Instance;
        var db = DbInstance.Create(
            "Main",
            cfg.Driver,
            cfg.Hostname,
            cfg.Port,
            cfg.Username,
            cfg.Password,
            cfg.DatabaseName,
            poolSize: cfg.PoolSize
        );

        using (var session = db.GetTlsSession())
        {
            Console.WriteLine("Hello, World!");
        }

        var nm = new TestModel();
        nm.age = 126;
        nm.name = "James";
        nm.Insert();

        nm.age = 621;
        nm.Update();

        TestModel.Delete((x) => x.name == "James");


        var minAge = 18;
        TestModel.Select((x) => (x.name.ToLower() != "bob" && x.age > minAge && x.age < 16) || (x.age * 2) < 21 || (x.name + "ppp" == "bobppp"));
        TestModel.Select((x) => (x.name != null && x.age > minAge && x.age < 16) || x.age < 21);
        TestModel.Select((x) => (null != x.name && x.age > minAge && x.age < 16) || x.age < 21);
        TestModel.Select((x) => ("bob" != x.name.ToLower() && x.age > 18 && x.age < 16) || x.age < 21);

        var t = TestModel.Select();
        foreach (TestModel testModel in t)
        {
            Console.WriteLine(testModel);
        }

        Models.Main.Public.AccountModel.Select();
        Models.Main.Public.AccountModel.Select();
        Models.Main.Public.AccountModel.Select();

        // See https://aka.ms/new-console-template for more information
        Logging.OnLogMessage += (l, x) => Console.WriteLine($"[Log][{l}] {x}");
        Logging.OnLogException += (l, x) => Console.WriteLine($"[Log][{l}] {x}");

        //var l_Blueprint = new RESTBlueprint();
        //l_Blueprint.AddRoutesOf<Routes.Book>();

        var restMainHandler = new ApiHttpHandler();
        restMainHandler.Hooks.AddLateRequestHook(new HttpServerExDbSessionReleaseHook());
        restMainHandler.Hooks.AddLateRequestHook(new TestHook());
        restMainHandler.MainBlueprint.AddBlueprint(Controllers.Book.Blueprint);
        restMainHandler.MainBlueprint.AddBlueprint(Controllers.Status.Blueprint);

        var httpServer = new HttpServerExCore("http://127.0.0.1:5001/", 4);
        httpServer.AddRequestHandler(restMainHandler);
        httpServer.Start();

        httpServer.Wait();
    }
}
