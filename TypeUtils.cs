using System.Collections.Generic;
using System.Linq;

public static class TypeUtils
{
    private class TypeInfo
    {
        public TypeInfo(string type, string defaultInitializer, string valueAccessor, string requiredUsing = "", bool requiresQuotes = false, string toStringConverter = "", string converterUsing = "", string assertPostfix = "")
        {
            Type = type;
            DefaultInitializer = defaultInitializer;
            ValueAccessor = valueAccessor;
            RequiredUsing = requiredUsing;
            RequiresQuotes = requiresQuotes;
            ToStringConverter = toStringConverter;
            ConverterUsing = converterUsing;
            AssertPostfix = assertPostfix;
        }

        public string Type { get; }
        public string DefaultInitializer { get; }
        public string ValueAccessor { get; }
        public string RequiredUsing { get; }
        public bool RequiresQuotes { get; }
        public string ToStringConverter { get; }
        public string ConverterUsing { get; }
        public string AssertPostfix { get; }
    }

    private static List<TypeInfo> types = new List<TypeInfo>
    {
        new TypeInfo("int", "", ".Value"),
        new TypeInfo("bool", "", ".Value"),
        new TypeInfo("string", " = \"\";", "", null, true),
        new TypeInfo("float", "", ".Value", "", false, ".ToString(CultureInfo.InvariantCulture)", "System.Globalization"),
        new TypeInfo("double", "", ".Value", "", false, ".ToString(CultureInfo.InvariantCulture)", "System.Globalization"),
        new TypeInfo("DateTime", "", ".Value", "System", true, ".ToString(\"o\")", "", ".Within(0.01).Seconds")
    };

    public static bool IsNullableRequiredForType(string type)
    {
        return types.Any(t => t.Type == type);
    }

    public static string GetInitializerForType(string type)
    {
        return Get(type).DefaultInitializer;
    }

    public static string GetValueAccessor(string type)
    {
        return Get(type).ValueAccessor;
    }

    public static string GetTypeRequiredUsing(string type)
    {
        return Get(type).RequiredUsing;
    }

    public static bool RequiresQuotes(string type)
    {
        return Get(type).RequiresQuotes;
    }

    public static string GetToStringConverter(string type)
    {
        return Get(type).ToStringConverter;
    }

    public static string GetConverterRequiredUsing(string type)
    {
        return Get(type).ConverterUsing;
    }

    public static string GetAssertPostfix(string type)
    {
        return Get(type).AssertPostfix;
    }

    private static TypeInfo Get(string type)
    {
        return types.Single(t => t.Type == type);
    }
}
