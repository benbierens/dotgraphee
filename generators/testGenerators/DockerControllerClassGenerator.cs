public class DockerControllerClassGenerator : BaseGenerator
{
    public DockerControllerClassGenerator(GeneratorConfig config)
    : base(config)
    {
    }
    
    public void CreateDockerControllerClass()
    {
        var fm = StartTestUtilsFile("DockerController");
        var cm = fm.AddClass("DockerController");
        cm.AddUsing("System");
        cm.AddUsing("System.Diagnostics");
        cm.AddUsing("System.Threading");
        cm.AddUsing("System.Threading.Tasks");
        cm.Modifiers.Clear();
        cm.Modifiers.Add("static");

        cm.AddClosure("public static async Task BuildImage()", liner =>
        {
            liner.Add("RunCommand(\"dotnet\", \"publish\", \"../../../../" + Config.Output.SourceFolder + "\", \"-c\", \"release\");");
            liner.Add("RunCommand(\"docker-compose\", \"up\", \"-d\");");
            liner.AddBlankLine();
            liner.Add("await Client.WaitUntilOnline();");
        });

        cm.AddClosure("public static async Task Up()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"up\", \"-d\");");
            liner.AddBlankLine();
            liner.Add("await Client.WaitUntilOnline();");
        });

        cm.AddClosure("public static void Down()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"down\");");
            liner.AddBlankLine();
            liner.Add("Thread.Sleep(TimeSpan.FromSeconds(1.0));");
        });

        cm.AddClosure("public static void ClearData()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"rm\", \"-s\", \"-v\", \"-f\", \"" + Config.Database.DbContainerName + "\");");
            liner.AddBlankLine();
            liner.Add("Thread.Sleep(TimeSpan.FromSeconds(1.0));");
        });

        cm.AddClosure("public static void DeleteImage()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"down\", \"--rmi\", \"all\", \"-v\");");
        });

        cm.AddClosure("private static void RunCommand(string cmd, params string[] args)", liner =>
        {
            liner.Add("var info = new ProcessStartInfo();");
            liner.Add("info.Arguments = string.Join(\" \", args);");
            liner.Add("info.FileName = cmd;");
            liner.Add("var p = Process.Start(info);");
            liner.Add("if (p == null) throw new Exception(\"Failed to start process.\");");
            liner.Add("p.WaitForExit();");
        });

        fm.Build();
    }
}
