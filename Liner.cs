using System.Collections.Generic;
using System.IO;

public class Liner
{
    private readonly List<string> lines = new List<string>();
    private int indent = 0;

    public void Indent()
    {
        indent++;
    }

    public void Deindent()
    {
        indent--;
    }

    public void StartClosure(string name)
    {
        Add(name);
        Add("{");
        Indent();
    }

    public void StartClosureInLine(string name)
    {
        Add(name + " {");
        Indent();
    }

    public void EndClosure(string additionalPostfix = "")
    {
        Deindent();
        Add("}" + additionalPostfix);
        AddBlankLine();
    }

    public void Add(string l)
    {
        var line = "";
        for (var i = 0; i < indent; i++) line += "    ";
        line += l;
        lines.Add(line);
    }

    public void AddBlankLine()
    {
        lines.Add("");
    }

    public string[] GetLines()
    {
        return lines.ToArray();
    }

    public void Write(string filename)
    {
        File.WriteAllLines(filename, lines);
    }
}
