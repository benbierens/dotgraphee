using System;

public class DatabaseGenerator : BaseGenerator
{
    public DatabaseGenerator(GeneratorConfig config)
        :base(config)
    {
    }

    public void GenerateDbContext()
    {
        MakeSrcDir(Config.Output.GeneratedFolder, Config.Output.DatabaseSubFolder);

        CreateDatabaseContextClass();
        CreateAccessClass();
    }

    public void CreateInitialMigration()
    {
        var s = Config.Output.SourceFolder;
        RunCommand("dotnet", "ef", "-p", s, "-s", s, "migrations", "add", "initial-setup");
    }

    private void CreateDatabaseContextClass()
    {
        var fm = StartSrcFile(Config.Output.DatabaseSubFolder, Config.Database.DbContextFileName);
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

        fm.Build();
    }

    public void CreateAccessClass()
    {
        var fm = StartSrcFile(Config.Output.DatabaseSubFolder, Config.Database.DbAccesserClassName);
        AddAccessInterface(fm);
        AddAccessClass(fm);
        fm.Build();
    }

    private void AddAccessInterface(FileMaker fm)
    {
        var im = fm.AddInterface("I" + Config.Database.DbAccesserClassName);
        im.AddUsing("System");
        im.AddUsing("System.Linq");

        AddLineWithClassConstraint(im, "T[] All<T>()");
        AddLineWithClassConstraint(im, "T? Single<T>(int id)");
        AddLineWithClassConstraint(im, "void Add<T>(T entity)");
        AddLineWithClassConstraint(im, "T? Update<T>(int id, Action<T> onEntity)");
        AddLineWithClassConstraint(im, "T? Delete<T>(int id)");
    }

    private void AddAccessClass(FileMaker fm)
    {
        var cm = fm.AddClass(Config.Database.DbAccesserClassName);
        cm.Modifiers.Clear();

        cm.AddInherrit("I" + Config.Database.DbAccesserClassName);

        AddClosureWithClassConstraint(cm, "public T[] All<T>()", liner =>
        {
            liner.Add("return GetDb().Set<T>().ToArray();");
        });

        AddClosureWithClassConstraint(cm, "public T? Single<T>(int id)", liner =>
        {
            liner.Add("return GetDb().Set<T>().Find(id);");
        });

        AddClosureWithClassConstraint(cm, "public void Add<T>(T entity)", liner =>
        {
            liner.Add("var db = GetDb();");
            liner.Add("db.Set<T>().Add(entity);");
            liner.Add("db.SaveChanges();");
        });


        AddClosureWithClassConstraint(cm, "public T? Update<T>(int id, Action<T> onEntity)", liner =>
        {
            liner.Add("var db = GetDb();");
            liner.Add("var entity = db.Find<T>(id);");
            liner.StartClosure("if (entity != null)");
            liner.Add("onEntity(entity);");
            liner.Add("db.SaveChanges();");
            liner.EndClosure();
            liner.Add("return entity;");
        });

        AddClosureWithClassConstraint(cm, "public T? Delete<T>(int id)", liner =>
        {
            liner.Add("var db = GetDb();");
            liner.Add("var entity = db.Find<T>(id);");
            liner.StartClosure("if (entity != null)");
            liner.Add("db.Set<T>().Remove(entity);");
            liner.Add("db.SaveChanges();");
            liner.EndClosure();
            liner.Add("return entity;");
        });

        cm.AddClosure("public static void EnsureCreated()", liner =>
        {
            liner.Add("var db = new DatabaseContext();");
            liner.Add("db.Database.EnsureCreated();");
        });

        cm.AddClosure("private DatabaseContext GetDb()", liner =>
        {
            liner.Add("return new DatabaseContext();");
        });
    }

    private void AddLineWithClassConstraint(ClassMaker im, string line)
    {
        im.AddLine(line + " where T : class;");
    }

    private void AddClosureWithClassConstraint(ClassMaker cm, string name, Action<Liner> inClosure)
    {
        cm.AddClosure(name + " where T : class", inClosure);
    }
}
