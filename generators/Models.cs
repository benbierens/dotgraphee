public class InputTypeNames
{
    public string Create { get; set; }
    public string Update { get; set; }
    public string Delete { get; set; }
}

public class ForeignProperty
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string WithId { get; set; }
    public bool IsSelfReference { get; set; }
    public bool IsPlural { get; set; }
    public bool IsOptional { get; set; }

    public override string ToString()
    {
        throw new System.Exception("Don't use this");
    }

    public bool IsRequiredSingular()
    {
        return !IsPlural && !IsOptional;
    }
}
