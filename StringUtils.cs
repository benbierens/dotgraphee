public static class StringUtils
{
    public static string FirstToLower(this string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0])) return str;
        return char.ToLower(str[0]) + str.Substring(1);
    }

    public static string FirstToUpper(this string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsUpper(str[0])) return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
}
