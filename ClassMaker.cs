using System;
using System.Collections.Generic;
using System.Linq;

public class ClassMaker
{
    private readonly string className;
    private readonly bool isInterface;
    private readonly List<string> lines = new List<string>();
    private readonly List<string> inherrit = new List<string>();
    private readonly List<string> usings = new List<string>();
    private readonly List<string> attributes = new List<string>();

    public ClassMaker(string className, bool isInterface = false)
    {
        this.className = className;
        this.isInterface = isInterface;
        Modifiers = new List<string>();
        Modifiers.Add("partial");
    }

    public List<string> Modifiers
    {
        get; private set;
    }

    public void AddLine(string line)
    {
        lines.Add(NormalizeWhitespaces(line));
    }

    public void AddBlankLine()
    {
        AddLine("");
    }

    public void BeginRegion(string name)
    {
        AddLine("#region " + name);
        AddBlankLine();
    }

    public void EndRegion()
    {
        AddLine("#endregion");
        AddBlankLine();
    }

    public PropertyMaker AddProperty(string name)
    {
        return new PropertyMaker(this, name);
    }

    public void AddInherrit(string name)
    {
        inherrit.Add(name);
    }

    public void AddUsing(string name)
    {
        if (!string.IsNullOrWhiteSpace(name) && !usings.Contains(name))
        {
            usings.Add(name);
        }
    }

    public void AddAttribute(string attribute)
    {
        attributes.Add(attribute);
    }

    public void AddClosure(string name, Action<Liner> inClosure, string additionalPostfix = "")
    {
        var liner = new Liner();
        liner.StartClosure(name);
        inClosure(liner);
        liner.EndClosure(additionalPostfix);
        
        lines.AddRange(liner.GetLines());
    }

    public void AddSubClass(string name, Action<ClassMaker> inClass)
    {
        var cm = new ClassMaker(name);
        inClass(cm);
        var liner = new Liner();
        cm.Write(liner);

        usings.AddRange(cm.GetUsings());
        lines.AddRange(liner.GetLines());
    }

    public string[] GetUsings()
    {
        return usings.ToArray();
    }

    public void Write(Liner liner)
    {
        Modifiers.Insert(0, "public");
        if (isInterface) Modifiers.Add("interface");
        else Modifiers.Add("class");

        var distinct = Modifiers.Distinct().ToArray();
        var modifiers = string.Join(" ", distinct);

        foreach (var att in attributes)
        {
            liner.Add("[" + att + "]");
        }
        
        liner.StartClosure(modifiers + " " + className + GetInherritTag());
        foreach (var line in lines) liner.Add(line);
        liner.EndClosure();
    }

    private string GetInherritTag()
    {
        if (!inherrit.Any()) return "";
        return " : " + string.Join(", ", inherrit);
    }

    private string NormalizeWhitespaces(string s)
    {
        while (s.Contains ("  "))
        {
            s = s.Replace("  ", " ");
        }
        return s;
    }
}