using System.Collections.Generic;

public class PropertyMaker
{
    private readonly List<string> modifiers = new List<string>();
    private readonly ClassMaker cm;
    private readonly string name;
    private string type;
    private bool isNullable;
    private bool isList;
    private bool isDbSet;
    private bool explicitNullInitializer;
    private bool noInitializer;
    private bool defaultInitializer;
    private string customInitializer;

    public PropertyMaker(ClassMaker cm, string name)
    {
        this.cm = cm;
        this.name = name;
    }

    public PropertyMaker IsType(string type)
    {
        this.type = type;
        this.isList = false;
        this.isDbSet = false;
        return this;
    }

    public PropertyMaker IsListOfType(string type)
    {
        this.type = type;
        this.isList = true;
        this.isDbSet = false;
        return this;
    }

    public PropertyMaker IsDbSetOfType(string type)
    {
        this.type = type;
        this.isList = false;
        this.isDbSet = true;
        return this;
    }

    public PropertyMaker WithModifier(string modifier)
    {
        modifiers.Add(modifier);
        return this;
    }

    public PropertyMaker IsNullable()
    {
        isNullable = true;
        return this;
    }

    public PropertyMaker InitializeAsExplicitNull()
    {
        explicitNullInitializer = true;
        return this;
    }

    public PropertyMaker NoInitializer()
    {
        noInitializer = true;
        return this;
    }

    public PropertyMaker DefaultInitializer()
    {
        defaultInitializer = true;
        return this;
    }

    public PropertyMaker WithCustomInitializer(string initializer)
    {
        customInitializer = initializer;
        return this;
    }

    public void Build()
    {
        cm.AddLine("public " + 
            GetModifiers() + " " +
            GetPropertyType() + " " +
            name + Pluralize() + GetAccessor() +
            GetInitializer());
    }

    private string GetModifiers()
    {
        return string.Join(" ", modifiers);
    }

    private string GetAccessor()
    {
        if (isDbSet) return " => ";
        return " { get; set; }";
    }

    private string Pluralize()
    {
        if (isList || isDbSet) return "s";
        return "";
    }

    private string GetPropertyType()
    {
        var t = type;
        if (isList) t = "List<" + t + ">";
        if (isDbSet) t = "DbSet<" + t + ">";
        if (isNullable) return t + "?";
        return t;
    }

    private string GetInitializer()
    {
        if (customInitializer != null) return customInitializer;
        if (noInitializer) return "";
        if (explicitNullInitializer) return " = null!;";
        if (defaultInitializer) return " = default(" + type + ")!;";
        if (isNullable) return "";
        if (isList) return " = new List<" + type + ">();";
        if (isDbSet) return "Set<" + type + ">();";
        if (TypeUtils.IsNullableRequiredForType(type)) return TypeUtils.GetInitializerForType(type);
        return " = new " + type + "();";
    }
}