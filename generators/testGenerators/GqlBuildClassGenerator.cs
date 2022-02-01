public class GqlBuildClassGenerator : BaseGenerator
{
    public GqlBuildClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateGqlBuildClass()
    {
        var fm = StartTestUtilsFile("GqlBuild");
        var cm = fm.AddClass("GqlBuild");
        cm.AddUsing("System");
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Globalization");
        cm.AddUsing("System.Linq");
        cm.AddUsing("System.Reflection");

        cm.AddLine("private string verb = \"_\";");
        cm.AddLine("private string target = \"_\";");
        cm.AddLine("private string input = \"\";");
        cm.AddLine("private string result = \"{}\";");

        cm.AddClosure("public static GqlBuild Query(string target)", liner =>
        {
            liner.Add("return Create(target, \"query\");");
        });

        cm.AddClosure("public static GqlBuild Mutation(string target)", liner =>
        {
            liner.Add("return Create(target, \"mutation\");");
        });

        cm.AddClosure("public static GqlBuild Subscription(string target)", liner =>
        {
            liner.Add("return Create(target, \"subscription\");");
        });


        cm.AddClosure("public GqlBuild WithId(" + Config.IdType + " id)", liner =>
        {
            if (Config.IdType == "int") liner.Add("input = \"(id: \" + ExpressValue(id) + \")\";");
            if (Config.IdType == "string") liner.Add("input = \"(id: \" + ExpressValueWithQuotes(id) + \")\";");
            liner.Add("return this;");
        });

        cm.AddClosure("public GqlBuild WithInput<T>(T inputObject) where T : class", liner =>
        {
            liner.Add("var fields = new List<string>();");
            liner.Add("var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);");
            liner.StartClosure("foreach (var p in properties)");
            liner.Add("if (IsPrimitive(p)) fields.Add(FormatPrimitive(inputObject, p));");
            liner.Add("if (IsString(p)) fields.Add(FormatString(inputObject, p));");
            liner.Add("if (IsDateTime(p)) fields.Add(FormatDateTime(inputObject, p));");
            liner.EndClosure();
            liner.Add("var f = string.Join(\" \", fields.Where(f => !string.IsNullOrWhiteSpace(f)));");
            liner.Add("input = \"(input: { \" + f + \" })\";");
            liner.Add("return this;");
        });

        cm.AddClosure("public GqlBuild WithOutput<T>()", liner =>
        {
            liner.Add("var fields = new List<string>();");
            liner.Add("var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);");
            liner.AddBlankLine();
            liner.StartClosure("foreach (var p in properties)");
            liner.StartClosure("if (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime))");
            liner.Add("fields.Add(FirstToLower(p.Name));");
            liner.EndClosure();
            liner.EndClosure();
            liner.Add("result = \"{\" + string.Join(\" \", fields) + \"}\";");
            liner.Add("return this;");
        });

        cm.AddClosure("public string Build()", liner =>
        {
            liner.Add("return \"{ \\\"query\\\": \\\"\" + verb + \" { \" + target + input + result + \" } \\\" }\";");
        });

        cm.AddClosure("private static GqlBuild Create(string target, string verb)", liner =>
        {
            liner.StartClosure("return new GqlBuild");
            liner.Add("target = target,");
            liner.Add("verb = verb");
            liner.EndClosure(";");
        });

        cm.AddClosure("private bool IsPrimitive(PropertyInfo p)", liner =>
        {
            liner.Add("if (p.PropertyType.IsPrimitive) return true;");
            liner.Add("var underlyingType = Nullable.GetUnderlyingType(p.PropertyType);");
            liner.Add("return underlyingType != null && underlyingType.IsPrimitive;");
        });

        cm.AddClosure("private bool IsString(PropertyInfo p)", liner =>
        {
            liner.Add("return p.PropertyType == typeof(string);");
        });

        cm.AddClosure("private bool IsDateTime(PropertyInfo p)", liner =>
        {
            liner.Add("return p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?);");
        });

        cm.AddClosure("private static string FormatPrimitive(object inputObject, PropertyInfo p)", liner =>
        {
            liner.Add("var value = p.GetValue(inputObject);");
            liner.Add("if (value == null) return \"\";");
            liner.Add("return GetFieldHeader(p) + \": \" + FirstToLower(ExpressValue(value));");
        });

        cm.AddClosure("private static string FormatString(object inputObject, PropertyInfo p)", liner =>
        {
            liner.Add("var value = p.GetValue(inputObject);");
            liner.Add("if (value == null) return \"\";");
            liner.Add("return GetFieldHeader(p) + \": \" + ExpressValueWithQuotes(value);");
        });

        cm.AddClosure("private static string FormatDateTime(object inputObject, PropertyInfo p)", liner =>
        {
            liner.Add("var value = p.GetValue(inputObject);");
            liner.Add("if (value == null) return \"\";");
            liner.Add("var dt = (DateTime)value;");
            liner.Add("return GetFieldHeader(p) + \": \" + ExpressValueWithQuotes(dt.ToString(\"o\"));");
        });

        cm.AddClosure("private static string GetFieldHeader(PropertyInfo p)", liner =>
        {
            liner.Add("return FirstToLower(p.Name);");
        });

        cm.AddClosure("private static string ExpressValue(object value)", liner =>
        {
            liner.Add("var s = Convert.ToString(value, CultureInfo.InvariantCulture);");
            liner.Add("if (s == null) return \"\";");
            liner.Add("return s;");
        });

        cm.AddClosure("private static string ExpressValueWithQuotes(object value)", liner =>
        {
            liner.Add("return \"\\\\\\\"\" + ExpressValue(value) + \"\\\\\\\"\";");
        });

        cm.AddClosure("private static string FirstToLower(string str)", liner =>
        {
            liner.Add("if (string.IsNullOrEmpty(str) || char.IsLower(str[0])) return str;");
            liner.Add("return char.ToLower(str[0]) + str.Substring(1);");
        });

        fm.Build();
    }
}
