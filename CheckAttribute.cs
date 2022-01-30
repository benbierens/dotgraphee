using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public enum CheckType
{
    NotEmpty,
    ParsesTo,
    OneOf,
    OneOfSupportedTypes
}

public class CheckAttribute : Attribute
{
    private Dictionary<CheckType, Action<List<string>, string, string>> checkers;

    public CheckAttribute(CheckType checkType)
    {
        CheckType = checkType;
        InitializeCheckers();
    }

    public CheckAttribute(CheckType checkType, Type enumType)
    {
        CheckType = checkType;
        EnumType = enumType;
        InitializeCheckers();
    }

    public CheckAttribute(CheckType checkType, params string[] options)
    {
        CheckType = checkType;
        Options = options;
        InitializeCheckers();
    }

    public CheckType CheckType { get; }
    public string[] Options { get; }
    public Type EnumType { get; }

    public void Validate(List<string> errors, string name, object value)
    {
        var s = value as string;
        checkers[CheckType](errors, name, s);
    }

    private void InitializeCheckers()
    {
        checkers = new Dictionary<CheckType, Action<List<string>, string, string>>
        {
            { CheckType.NotEmpty, NotEmpty },
            { CheckType.ParsesTo, ParsesTo },
            { CheckType.OneOf, OneOf },
            { CheckType.OneOfSupportedTypes , OneOfSupportedTypes },
        };
    }

    private void NotEmpty(List<string> errors, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(GetPrefix(name, value) + "Field cannot be empty.");
        }
    }

    private void ParsesTo(List<string> errors, string name, string value)
    {
        if (!Enum.TryParse(EnumType, value, out var result))
        {
            errors.Add(GetPrefix(name, value) + "Invalid. Options are: " + GetEnumOptions());
        }
    }

    private void OneOf(List<string> errors, string name, string value)
    {
        if (!Options.Contains(value))
        {
            errors.Add(GetPrefix(name, value) + "Invalid. Options are: " + string.Join(", ", Options.Select(Surround)));
        }
    }

    private void OneOfSupportedTypes(List<string> errors, string name, string value)
    {
        var types = TypeUtils.GetSupportedTypes();
        if (!types.Contains(value))
        {
            errors.Add(GetPrefix(name, value) + "Invalid. Options are: " + string.Join(", ", types.Select(Surround)));
        }
    }

    private string GetPrefix(string name, string value)
    {
        return "[" + name + " = '" + value + "'] ";
    }

    private string GetEnumOptions()
    {
        var members = EnumType.GetMembers(BindingFlags.Public | BindingFlags.Static);
        return string.Join(", ", members.Select(m => Surround(m.Name)));
    }

    private string Surround(string s)
    {
        return "'" + s + "'";
    }
}
