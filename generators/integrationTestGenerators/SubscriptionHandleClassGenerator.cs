using System;

public class SubscriptionHandleClassGenerator : BaseGenerator 
{
    public SubscriptionHandleClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateSubscriptionHandleClass()
    {
        var fm = StartIntegrationTestUtilsFile("SubscriptionHandle");
        var im = fm.AddInterface("ISubscriptionHandle");
        im.AddLine("Task Subscribe<TOutput>();");
        im.AddLine("Task Unsubscribe();");

        var cm = fm.AddClass("SubscriptionHandle<T>");
        cm.AddInherrit("ISubscriptionHandle");
        cm.AddUsing("System");
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Linq");
        cm.AddUsing("System.Net.WebSockets");
        cm.AddUsing("System.Text");
        cm.AddUsing("System.Threading");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("Newtonsoft.Json");
        cm.AddUsing("NUnit.Framework");

        cm.AddLine("private readonly string subscription;");
        cm.AddLine("private readonly CancellationTokenSource cts = new CancellationTokenSource();");
        cm.AddLine("private readonly ClientWebSocket ws = new ClientWebSocket();");
        cm.AddLine("private readonly List<string> received = new List<string>();");
        cm.AddLine("private bool running;");
        cm.AddBlankLine();

        cm.AddClosure("public SubscriptionHandle(string subscription)", liner => 
        {
            liner.Add("this.subscription = subscription;");
            liner.Add("running = true;");
            liner.Add("ws.Options.AddSubProtocol(\"graphql-ws\");");
        });

        cm.AddClosure("public async Task Subscribe<TOutput>()", liner => 
        {
            liner.Add("await ws.ConnectAsync(new Uri(Client.WsUrl), cts.Token);");
            liner.Add("var _ = Task.Run(ReceivingLoop);");
            liner.Add("var query = GqlBuild.Subscription(subscription).WithOutput<TOutput>().Build();");
            liner.Add("await Send(\"{type: \\\"connection_init\\\", payload: {}}\");");
            liner.Add("await Send(\"{\\\"id\\\":\\\"1\\\",\\\"type\\\":\\\"start\\\",\\\"payload\\\":\" + query + \"}\");");
        });

        cm.AddClosure("public async Task Unsubscribe()", liner => 
        {
            liner.Add("running = false;");
            liner.Add("await Send(\"{\\\"id\\\":\\\"1\\\",\\\"type\\\":\\\"stop\\\"}\");");
        });

        cm.AddClosure("public T AssertReceived()", liner => 
        {
            liner.Add("var line = GetSubscriptionLine();");
            liner.StartClosure("if (line.Contains(\"errors\"))");
            liner.Add("Assert.Fail(\"Response contains errors:\" + line);");
            liner.Add("throw new Exception();");
            liner.EndClosure();

            liner.Add("var response = JsonConvert.DeserializeObject<SubscriptionResponse<T>>(line);");
            liner.Add("return response.Payload.Data;");
        });

        cm.AddClosure("private string GetSubscriptionLine()", liner =>
        {
            liner.Add("var start = DateTime.Now;");
            liner.StartClosure("while ((DateTime.Now - start) < TimeSpan.FromSeconds(15))");
            liner.Add("var line = received.SingleOrDefault(l => l.Contains(subscription));");
            liner.Add("if (line != null) return line;");
            liner.Add("Thread.Sleep(100);");
            liner.EndClosure();
            AddSubscriptionDebugLine(liner);
            liner.Add("Assert.Fail(\"Expected subscription '\" + subscription + \"', but was not received.\");");
            liner.Add("throw new Exception();");
        });

        cm.AddClosure("private async Task ReceivingLoop()", liner => 
        {
            liner.StartClosure("while (running)");
            liner.Add("var bytes = new byte[1024];");
            liner.Add("var buffer = new ArraySegment<byte>(bytes);");
            liner.Add("var receive = await ws.ReceiveAsync(buffer, cts.Token);");
            liner.Add("var l = bytes.Take(receive.Count).ToArray();");
            liner.Add("var line = Encoding.UTF8.GetString(l);");
            liner.Add("received.Add(line);");
            liner.Add("TestContext.WriteLine(\"Subscription channel received: \" + line);");
            liner.EndClosure();
        });

        cm.AddClosure("private async Task Send(string query)", liner => 
        {            
            liner.Add("TestContext.WriteLine(\"Subscription channel send: '\" + query + \"'\");");
            liner.Add("var qbytes = Encoding.UTF8.GetBytes(query);");
            liner.Add("var segment= new ArraySegment<byte>(qbytes);");
            liner.Add("await ws.SendAsync(segment, WebSocketMessageType.Text, true, cts.Token);");
        });

        AddSubscriptionResponseClass(fm);
        AddPayloadClass(fm);

        fm.Build();
    }

    private void AddPayloadClass(FileMaker fm)
    {
        var cm = fm.AddClass("Payload<T>");
        cm.AddProperty("Data")
            .IsType("T")
            .DefaultInitializer()
            .Build();
    }

    private void AddSubscriptionResponseClass(FileMaker fm)
    {
        var cm = fm.AddClass("SubscriptionResponse<T>");
        cm.AddProperty("Payload")
            .IsType("Payload<T>")
            .InitializeAsExplicitNull()
            .Build();
    }

    private void AddSubscriptionDebugLine(Liner liner)
    {
        liner.Add("TestContext.WriteLine(\"Subscription channel received: \" + string.Join(Environment.NewLine, received));");
    }
}