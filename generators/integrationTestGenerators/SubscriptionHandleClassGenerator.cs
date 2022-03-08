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
        im.AddLine("void Unsubscribe();");

        var cm = fm.AddClass("SubscriptionHandle<TResult>");
        cm.AddInherrit("ISubscriptionHandle where TResult : class");
        cm.AddUsing("System");
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Linq");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("StrawberryShake");
        cm.AddUsing(Config.GenerateNamespace + ".Client");

        cm.AddLine($"private readonly I{ClientName} client;");
        cm.AddLine("private readonly List<TResult> received = new List<TResult>();");
        cm.AddLine("private readonly List<string> errors = new List<string>();");
        cm.AddLine("private IDisposable handle = null!;");
        cm.AddBlankLine();

        cm.AddClosure($"public SubscriptionHandle(I{ClientName} client)", liner =>
        {
            liner.Add("this.client = client;");
        });

        cm.AddClosure($"public void Subscribe(Func<I{ClientName}, IObservable<IOperationResult<TResult>>> subSelector)", liner => 
        {
            liner.Add("var observable = subSelector(client);");
            liner.Add("handle = observable.Subscribe(new SubscriptionListener<TResult>(received, errors));");
        });

        cm.AddClosure("public void Unsubscribe()", liner => 
        {
            liner.Add("handle.Dispose();");
        });

        cm.AddClosure("public TResult AssertReceived()", liner => 
        {
            liner.StartClosure("if (errors.Any())");
            liner.Add("Assert.Fail(\"Response contains errors: \" + string.Join(\", \", errors));");
            liner.Add("throw new Exception();");
            liner.EndClosure();

            liner.StartClosure("if (!received.Any())");
            liner.Add("Assert.Fail(\"Expected subscription of type '\" + typeof(TResult) + \"', but was not received..\");");
            liner.EndClosure();

            liner.Add("return received.Last();");
        });

        AddSubscriptionListenerClass(fm);

        fm.Build();
    }


    //public class SubscriptionListener<TResult> : IObserver<IOperationResult<TResult>> where TResult : class
    //{
  
    //}

    private void AddSubscriptionListenerClass(FileMaker fm)
    {
        var cm = fm.AddClass("SubscriptionListener<TResult>");
        cm.AddInherrit("IObserver<IOperationResult<TResult>> where TResult : class");

        cm.AddLine("private readonly List<TResult> entities;");
        cm.AddLine("private readonly List<string> errors;");

        cm.AddClosure("public SubscriptionListener(List<TResult> entities, List<string> errors)", liner =>
        {
            liner.Add("this.entities = entities;");
            liner.Add("this.errors = errors;");
        });

        cm.AddClosure("public void OnCompleted()", liner => { });

        cm.AddClosure("public void OnError(Exception error)", liner => 
        {
            liner.Add("errors.Add(error.ToString());");
        });

        cm.AddClosure("public void OnNext(IOperationResult<TResult> value)", liner =>
        {
            liner.Add("var entity = value.Data;");
            liner.StartClosure("if (entity != null)");
            liner.Add("entities.Add(entity);");
            liner.EndClosure();
        });
    }
}