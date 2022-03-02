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

        cm.AddLine("private static String ApplicationContainerName = \"graphql\";");
        cm.AddBlankLine();

        cm.AddClosure("public static async Task BuildImage()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"build\", ApplicationContainerName);");
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

        cm.AddClosure("public static void Restart()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"restart\", \"-t 0\", ApplicationContainerName);");
            liner.AddBlankLine();
            liner.Add("Thread.Sleep(TimeSpan.FromSeconds(1.0));");
        });

        cm.AddClosure("public static void ClearData()", liner =>
        {
            liner.Add("var TruncateAllTables = @\"do");
            liner.Indent();
            liner.Add("$$");
            liner.Add("declare");
            liner.Indent();
            liner.Add("truncate_all text;");
            liner.Deindent();
            liner.Add("begin");
            liner.Indent();
            liner.Add("SELECT 'TRUNCATE ' || string_agg(format('%I.%I', schemaname, tablename), ',') || ' RESTART IDENTITY CASCADE;'");
            liner.Add("INTO truncate_all");
            liner.Add("from pg_tables");
            liner.Add("where schemaname in ('public');");
            liner.Add("execute truncate_all;");
            liner.Deindent();
            liner.Add("end");
            liner.Add("$$\";");

            liner.Add("RunCommand(\"docker-compose\", \"exec\", \"" + Config.Database.DbContainerName + "\", \"psql\", \"--user=" + Config.Database.Docker.DbUsername + "\", \"" + Config.Database.Docker.DbName + "\", \"-q\", \"-c\", \"\\\"\" + TruncateAllTables + \"\\\"\");");
            liner.AddBlankLine();
            liner.Add("Thread.Sleep(TimeSpan.FromSeconds(1.0));");
        });

        cm.AddClosure("public static void DeleteImage()", liner =>
        {
            liner.Add("RunCommand(\"docker-compose\", \"down\", \"--rmi\", ApplicationContainerName, \"-v\");");
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
