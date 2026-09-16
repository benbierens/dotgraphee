using System;

public class SubscriptionHandleClassGenerator : BaseGenerator 
{
    public SubscriptionHandleClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateSubscriptionHandleClass()
    {
        var fm = StartClientFile("SubscriptionHandle");
        var im = fm.AddInterface("ISubscriptionHandle");
        im.AddLine("Task Unsubscribe();");
        im.AddLine("string[] GetErrors();");

        var cm = fm.AddClass("SubscriptionHandle<T>");
        cm.AddInherrit("ISubscriptionHandle");
        cm.AddInherrit("IAsyncDisposable");
        cm.AddUsing("System");
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Linq");
        cm.AddUsing("System.Net.WebSockets");
        cm.AddUsing("System.Text");
        cm.AddUsing("System.Threading");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("Newtonsoft.Json");

        cm.AddLine("private readonly string subscriptionId = Guid.NewGuid().ToString();");
        cm.AddLine("private readonly string subscription;");
        cm.AddLine("private readonly List<string> errors = new List<string>();");
        cm.AddLine("private readonly Lock payloadHandlerLock = new Lock();");
        cm.AddLine("private readonly Action<T> onPayload;");
        cm.AddLine("private readonly CancellationTokenSource cts = new CancellationTokenSource();");
        cm.AddLine("private readonly ClientWebSocket ws = new ClientWebSocket();");
        cm.AddLine("private readonly SemaphoreSlim lifecycleLock = new SemaphoreSlim(1, 1);");
        cm.AddLine("private readonly TaskCompletionSource<bool> acknowledged = CreateCompletionSource();");
        cm.AddLine("private readonly TaskCompletionSource<bool> completed = CreateCompletionSource();");
        cm.AddLine("private Task receiving = Task.CompletedTask;");
        cm.AddLine("private bool subscribed;");
        cm.AddLine("private bool stopped;");
        cm.AddBlankLine();

        cm.AddClosure("public SubscriptionHandle(string subscription, Action<T> onPayload)", liner => 
        {
            liner.Add("this.subscription = subscription;");
            liner.Add("this.onPayload = onPayload;")
            liner.Add("running = true;");
            liner.Add("ws.Options.AddSubProtocol(\"graphql-ws\");");
        });

        cm.AddClosure("public async Task Subscribe<TOutput>()", liner => 
        {
            liner.Add("await lifecycleLock.WaitAsync();");
            liner.StartClosure("try");
            liner.StartClosure("if (subscribed || stopped)");
            liner.Add("throw new InvalidOperationException(\"Can only subscribe once.\")");
            liner.EndClosure();

            liner.Add("await ws.ConnectAsync(new Uri(Client.WsUrl), cts.Token);");
            liner.Add("receiving = ReceiveMessages();");
            liner.Add("await Send(\"{type: \\\"connection_init\\\", payload: {}}\");");
            liner.Add("await acknowledged.Task.WaitAsync(cts.Token);");
            liner.AddBlankLine();

            liner.Add("var query = GqlBuild.Subscription(subscription).WithOutput<TOutput>().Build();");
            liner.Add("await Send(\"{\\\"id\\\":\\\"\" + subscriptionId + \"\\\",\\\"type\\\":\\\"start\\\",\\\"payload\\\":\" + query + \"}\");");
            liner.Add("subscribed = true;");
            liner.EndClosure();

            liner.StartClosure("catch");
            liner.Add("await Shutdown();");
            liner.Add("throw;");
            liner.EndClosure();

            liner.StartClosure("finally");
            liner.Add("lifecycleLock.Release();");
            liner.EndClosure();
        });

        cm.AddClosure("public async Task Unsubscribe()", liner => 
        {
            liner.Add("await lifecycleLock.WaitAsync();");
            liner.StartClosure("try");
            liner.StartClosure("if (stopped)");
            liner.Add("return;");
            liner.EndClosure();

            liner.Add("stopped = true;");
            liner.StartClosure("subscribed && ws.State == WebSocketState.Open");
            liner.Add("await Send(\"{\\\"id\\\":\\\"\" + subscriptionId + \"\\\",\\\"type\\\":\\\"stop\\\"}\");");
            liner.Add("await WaitForCompletion();");
            liner.EndClosure();

            liner.Add("await Shutdown();");
            liner.EndClosure();

            liner.StartClosure("finally");
            liner.Add("lifecycleLock.Release();");
            liner.EndClosure();
        });

        cm.AddClosure("public string[] GetErrors()", liner =>
        {
            liner.Add("return errors.ToArray();");
        });

        cm.AddClosure("public async ValueTask DisposeAsync()", liner =>
        {
            liner.Add("await Unsubscribe();");
        });

        cm.AddClosure("private async Task ReceiveMessages()", liner => 
        {
            liner.StartClosure("try");
            liner.StartClosure("while (!cts.IsCancellationRequested)");
            liner.Add("var line = await ReceiveMessage();");
            liner.StartClosure("if (line == null)");
            liner.Add("errors.Add(\"Subscription socket closed unexpectedly.\");");
            liner.Add("throw new InvalidOperationException(\"Subscription socket closed unexpectedly.\");");
            liner.EndClosure();
            liner.Add("HandleMessage(line);");
            liner.EndClosure();
            liner.EndClosure();
            liner.StartClosure("catch (OperationCanceledException) when (cts.IsCancellationRequested)");
            liner.EndClosure();
            liner.StartClosure("catch (Exception exception)");
            liner.Add("acknowledged.TrySetException(exception);");
            liner.Add("completed.TrySetException(exception);");
            liner.EndClosure();
        });

        cm.AddClosure("private async Task<string?> ReceiveMessage()", liner =>
        {
            liner.Add("using var message = new MemoryStream();");
            liner.Add("var bytes = new byte[4096];");
            liner.Add("WebSockerReceiveResult result");
            liner.StartClosure("do");
            liner.Add("result = await ws.ReceiveAsync(new ArraySegment<byte>(bytes), cts.Token);");
            liner.StartClosure("if (result.MessageType == WebSocketMessageType.Close)");
            liner.Add("return null;");
            liner.EndClosure();
            liner.Add("message.Write(bytes, 0, result.Count);");
            liner.EndClosure();
            liner.Add("while (!result.EndOfMessage);");
            liner.Add("return Encoding.UTF8.GetString(message.ToArray());");
        });

        cm.AddClosure("private void HandleMessage(string line)", liner =>
        {
            liner.Add("var message = JObject.Parse(line);");
            liner.StartClosure("switch (message[\"type\"]?.Value<string>())");
            
            liner.Add("case \"connection_ack\":");
            liner.Indent();
            liner.Add("acknowledged.TrySetResult(true);");
            liner.Add("break;");
            liner.Deindent();

            liner.Add("case \"connection_error\":");
            liner.Indent();
            liner.Add("var error = message[\"payload\"]?.ToString() ?? line;");
            liner.Add("var exception = new InvalidOperationException(\"Subscription error: \" + error);");
            liner.Add("acknowledged.TrySetException(exception);");
            liner.Add("completed.TrySetException(exception);");
            liner.Add("break;");
            liner.Deindent();

            liner.Add("case \"data\":");
            liner.Add("case \"error\":");
            liner.Indent();
            liner.Add("HandlePayload(message, line);");
            liner.Add("break;");
            liner.Deindent();

            liner.Add("case \"complete\":");
            liner.Indent();
            liner.Add("completed.TrySetResult(true);");
            liner.Add("break;");
            liner.Deindent();

            liner.EndClosure();
        });

        cm.AddClosure("private void HandlePayload(JObject message, string line)", liner =>
        {
            liner.StartClosure("if (message[\"type\"]?.Value<string>() == \"error\" || message[\"payload\"]?[\"errors\"] != null)");
            liner.Add("errors.Add(\"Response contains errors: \" + line);");
            liner.Add("return");
            liner.EndClosure();
            liner.Add("var response = JsonConvert.DeserializeObject<SubscriptionResponse<T>>(line)!;");
            liner.StartClosure("lock (payloadHandlerLock)");
            liner.Add("onPayload(response.Payload.Data);");
            liner.EndClosure();
        });

        cm.AddClosure("private async Task Send(string message)", liner => 
        {            
            liner.Add("var bytes = Encoding.UTF8.GetBytes(message);");
            liner.Add("await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);");
        });

        cm.AddClosure("private async Task WaitForCompletion()", liner =>
        {
            liner.StartClosure("try");
            liner.Add("await Task.WhenAny(completed.Task).WaitAsync(TimeSpan.FromSeconds(5));");
            liner.EndClosure();
            liner.StartClosure("catch (TimeoutException)");
            liner.EndClosure();
        });

        cm.AddClosure("private async Task Shutdown()", liner =>
        {
            liner.Add("stopped = true;");
            liner.Add("cts.Cancel();");
            liner.StartClosure("if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)");
            liner.Add("await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, \"Unsubscribed\", CancellationToken.None);");
            liner.EndClosure();

            liner.StartClosure("try");
            liner.Add("await receiving;");
            liner.EndClosure();
            liner.StartClosure("catch (OperationCanceledException)");
            liner.EndClosure();

            liner.Add("ws.Dispose();");
        });

        cm.AddClosure("private static TaskCompletionSource<bool> CreateCompletionSource()", liner =>
        {
            liner.Add("return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);");
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
}