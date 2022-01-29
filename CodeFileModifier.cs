using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CodeFileModifier
{
    private readonly string filename;
    private readonly List<string> lines;

    public CodeFileModifier(string filename)
    {
        this.filename = filename;

        lines = File.ReadAllLines(filename).ToList();
    }

    public void AddUsing(string usingNamespace)
    {
        lines.Insert(0, "using " + usingNamespace + ";");
    }

    public void Insert(int lineNumber, int indentLevel, string line)
    {
        var indent = "";
        for (var i = 0; i < indentLevel; i++) indent += "    ";
        lines.Insert(lineNumber, indent + line);
    }

    public void ReplaceLine(string original, params string[] updated)
    {
        var source = lines.ToArray();
        for (var i = 0; i < source.Length; i++)
        {
            var line = source[i];
            if (line.Contains(original))
            {
                var start = line.IndexOf(original);
                var padding = line.Substring(0, start);

                lines.RemoveAt(i);
                var j = 0;
                foreach (var update in updated)
                {
                    lines.Insert(i + j, padding + update);
                    j++;
                }
                return;
            }
        }
    }

    public void Modify()
    {
       File.WriteAllLines(filename, lines);
    }

}