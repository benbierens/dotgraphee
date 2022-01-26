public class ClientClassGenerator : BaseGenerator
{
    public ClientClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateClientClass()
    {
        var fm = StartTestUtilsFile("Client");
        var cm = fm.AddClass("Client");
        cm.Modifiers.Clear();
        cm.Modifiers.Add("static");
        cm.AddUsing("Newtonsoft.Json");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System");
        cm.AddUsing("System.Net.Http");
        cm.AddUsing("System.Text");
        cm.AddUsing("System.Threading");
        cm.AddUsing("System.Threading.Tasks");

        cm.AddLine("private static readonly HttpClient http = new HttpClient();");
        cm.AddLine("private static readonly string url = \"://localhost/graphql\";");
        cm.AddBlankLine();

        cm.AddClosure("public static async Task<T> PostRequest<T>(string query)", liner =>
        {
            liner.Add("TestContext.WriteLine(\"Request: '\" + query + \"'\");");
            liner.Add("var content = await HttpPost(query);");
            liner.Add("TestContext.WriteLine(\"Response: '\" + content + \"'\");");
            liner.Add("var result = JsonConvert.DeserializeObject<GqlData<T>>(content);");
            liner.Add("if (result.Data == null) throw new Exception(\"GraphQl operation failed. Query: '\" + query + \"' Response: '\" + content + \"'\");");
            liner.Add("return result.Data;");
        });

        cm.AddClosure("private static async Task<string> HttpPost(string query)", liner =>
        {
            liner.Add("var response = await http.PostAsync(HttpUrl, new StringContent(query, Encoding.UTF8, \"application/json\"));");
            liner.Add("return await response.Content.ReadAsStringAsync();");
        });

        cm.AddClosure("public static string HttpUrl", liner =>
        {
            liner.Add("get { return \"http\" + url; }");
        });

        cm.AddClosure("public static string WsUrl", liner =>
        {
            liner.Add("get { return \"ws\" + url; }");
        });

        cm.AddClosure("public static async Task WaitUntilOnline()", liner =>
        {
            liner.Add("var start = DateTime.Now;");
            liner.StartClosure("while ((DateTime.Now - start) < TimeSpan.FromSeconds(15))");
            liner.Add("var isOnline = await IsOnline();");
            liner.Add("if (isOnline) return;");
            liner.Add("Thread.Sleep(100);");
            liner.EndClosure();
            liner.Add("throw new TimeoutException(\"Server didn't come online within 15 seconds.\");");
        });

        cm.AddClosure("public static async Task<bool> IsOnline()", liner =>
        {
            liner.StartClosure("try");
            liner.Add("var query = \"{ \\\"query\\\": \\\"query IntrospectionQuery {__schema { queryType { name } mutationType { name } subscriptionType { name } } } \\\" }\";");
            liner.Add("var result = await HttpPost(query);");
            liner.Add("return result != null;");
            liner.EndClosure();
            liner.StartClosure("catch");
            liner.Add("return false;");
            liner.EndClosure();
        });

        fm.Build();
    }
}
