public class DatabaseGenerator : BaseGenerator
{
    public DatabaseGenerator(GeneratorConfig config)
        :base(config)
    {
    }

    public void GenerateDbContext()
    {
        MakeSrcDir(Config.Output.GeneratedFolder, Config.Output.DatabaseSubFolder);

        var fm = StartSrcFile(Config.Output.DatabaseSubFolder, Config.Database.DbContextFileName);
        AddDatabaseContextClass(fm);
        AddStaticAccessClass(fm);

        fm.Build();
    }

    public void CreateInitialMigration()
    {
        var s = Config.Output.SourceFolder;
        RunCommand("dotnet", "ef", "-p", s, "-s", s, "migrations", "add", "initial-setup");
    }

    private void AddDatabaseContextClass(FileMaker fm)
    {
        var cm = StartClass(fm, Config.Database.DbContextClassName);

        cm.AddUsing("System");
        cm.AddUsing("Microsoft.EntityFrameworkCore");

        cm.AddInherrit("DbContext");

        foreach (var m in Models)
        {
            cm.AddProperty(m.Name)
                .IsDbSetOfType(m.Name)
                .Build();
        }

        cm.AddBlankLine();

        cm.AddClosure("private string GetEnvOrDefault(string env, string defaultValue)", liner => {
            liner.Add("var value = Environment.GetEnvironmentVariable(env);");
            liner.Add("if (value == null) return defaultValue;");
            liner.Add("return value;");
        });

        cm.AddClosure("protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)", liner =>
        {
            var localDev = Config.Database.LocalDev;
            liner.Add("var dbHost = GetEnvOrDefault(\"DB_HOST\", \"" + localDev.DbHost + "\");");
            liner.Add("var dbName = GetEnvOrDefault(\"DB_DATABASENAME\", \"" + localDev.DbName + "\");");
            liner.Add("var dbUsername = GetEnvOrDefault(\"DB_USERNAME\", \"" + localDev.DbUsername + "\");");
            liner.Add("var dbPassword = GetEnvOrDefault(\"DB_PASSWORD\", \"" + localDev.DbPassword + "\");");
            liner.Add("var connectionString = \"Host=\" + dbHost + \";Database=\" + dbName + \";Username=\" + dbUsername + \";Password=\" + dbPassword;");

            liner.Add("");
            liner.Add("optionsBuilder");
            liner.Indent();
            liner.Add(".UseLazyLoadingProxies()");
            liner.Add(".UseNpgsql(connectionString);");
            liner.Deindent();
        });
    }

    public void AddStaticAccessClass(FileMaker fm)
    {
        var cm = fm.AddClass(Config.Database.DbAccesserClassName);
        cm.Modifiers.Clear();
        cm.Modifiers.Add("static");

        cm.AddClosure("public static " + Config.Database.DbContextClassName + " Context", liner => 
        {
            liner.StartClosure("get");
            liner.Add("return new " + Config.Database.DbContextClassName + "();");
            liner.EndClosure();
        });
    }
}
