public class AssertMaker
{
    private readonly BaseGenerator baseGenerator;
    private readonly Liner liner;
    private readonly string target;

    public AssertMaker(BaseGenerator baseGenerator, Liner liner, string target)
    {
        this.baseGenerator = baseGenerator;
        this.liner = liner;
        this.target = target;
    }
    
    public void EntityField(GeneratorConfig.ModelConfig m, string errorMessage)
    {
        foreach (var f in m.Fields)
        {
            EqualsTestEntity(m, f, errorMessage);
        }
        var foreignProperties = baseGenerator.GetForeignProperties(m);
        foreach (var f in foreignProperties)
        {
            if (!f.IsSelfReference)
            {
                ForeignIdEquals(m, f, errorMessage);
            }
        }
    }

    public void EqualsTestEntity(GeneratorConfig.ModelConfig m, GeneratorConfig.ModelField f, string errorMessage)
    {
        var target = m.Name + "." + f.Name;
        AddAssertEquals(m, f, target, errorMessage);
    }

    public void IdEquals(GeneratorConfig.ModelConfig m, string errorMessage)
    {
        liner.Add("Assert.That(" + target + ".Id, Is.EqualTo(TestData.Test" + m.Name + ".Id)," + FormatErrorMessage(m, "Id", errorMessage) + ");");
    }

    public void EqualsTestScalar(GeneratorConfig.ModelConfig m, GeneratorConfig.ModelField f, string errorMessage)
    {
        var target = f.Type.FirstToUpper();
        AddAssertEquals(m, f, target, errorMessage);
    }

    public void ForeignIdEquals(GeneratorConfig.ModelConfig m, ForeignProperty f, string errorMessage)
    {
        liner.Add("Assert.That(" + target + "." + f.WithId + ", Is.EqualTo(TestData.Test" + f.Type + ".Id)," + FormatErrorMessage(m, f, errorMessage) + ");");
    }

    public void CollectionOne(GeneratorConfig.ModelConfig m, string collectionName)
    {
        liner.Add("Assert.That(" + collectionName + ".Count, Is.EqualTo(1), \"Expected only 1 " + m.Name + "\");");
    }

    public void CollectionEmpty(GeneratorConfig.ModelConfig m, string collectionName)
    {
        liner.Add("CollectionAssert.IsEmpty(" + collectionName + ", \"Expected 0 " + m.Name + ".\");");
    }

    public void ErrorMessage(GeneratorConfig.ModelConfig m, string idTag)
    {
        var expectedErrorMessage = "Unable to find '" + m.Name + "' by Id: '\" + " + idTag + " + \"'";
        liner.Add("Assert.That(errors[0].Message, Is.EqualTo(\"" + expectedErrorMessage + "\"), \"Unexpected error message.\");");
    }

    public void FailedToFindMutationResponse(GeneratorConfig.ModelConfig m, string mutation)
    {
        if (baseGenerator.IsFailedToFindStrategyErrorCode())
        {
            CollectionOne(m, "errors");
            ErrorMessage(m, "TestData.Test" + m.Name + ".Id");
        }
        if (baseGenerator.IsFailedToFindStrategyNullObject())
        {
            NoErrors(mutation);
            NullReturned(m, mutation);
        }
    }

    public void FailedToFindQueryResponse(GeneratorConfig.ModelConfig m)
    {
        if (baseGenerator.IsFailedToFindStrategyErrorCode())
        {
            CollectionOne(m, "errors");
            ErrorMessage(m, "TestData.Test" + baseGenerator.Config.IdType.FirstToUpper());
        }
        if (baseGenerator.IsFailedToFindStrategyNullObject())
        {
            NoErrors("query");
            NullReturned(m, "");
        }
    }

    public void NoErrors(string queryOrMutation)
    {
        liner.Add("CollectionAssert.IsEmpty(errors, \"Expected " + queryOrMutation + " to not return errors.\");");
    }

    public void NullReturned(GeneratorConfig.ModelConfig m, string mutation)
    {
        var field = mutation + m.Name;
        liner.Add("Assert.That(gqlData.Data." + field + ", Is.Null, \"Expected null object to be returned.\");");
    }

    public void NoErrors()
    {
        liner.Add("gqlData.AssertNoErrors();");
    }

    public void DeleteResponse(GeneratorConfig.ModelConfig m)
    {
        var field = baseGenerator.Config.GraphQl.GqlMutationsDeleteMethod + m.Name;
        if (baseGenerator.IsFailedToFindStrategyNullObject())
        {
            liner.Add("if (response.Data." + field + " == null) throw new AssertionException(\"Unexpected null returned by " + 
                baseGenerator.Config.GraphQl.GqlMutationsDeleteMethod + " mutation.\");");
        }
        liner.Add("Assert.That(response.Data." + field + ".Id, Is.EqualTo(TestData.Test" + m.Name + ".Id), \"Incorrect Id returned by " + 
            baseGenerator.Config.GraphQl.GqlMutationsDeleteMethod + " mutation.\");");
    }

    public void EntityNotNull(string operation)
    {
        liner.Add("if (" + target + " == null) throw new AssertionException(\"Unexpected null returned by '" + operation + "'.\");");
    }

    private string FormatErrorMessage(GeneratorConfig.ModelConfig m, GeneratorConfig.ModelField f, string errorMessage)
    {
        return FormatErrorMessage(m, f.Name, errorMessage);
    }

    private string FormatErrorMessage(GeneratorConfig.ModelConfig m, ForeignProperty f, string errorMessage)
    {
        return FormatErrorMessage(m, f.Name, errorMessage);
    }

    private string FormatErrorMessage(GeneratorConfig.ModelConfig m, string f, string errorMessage)
    {
        return " \"" + errorMessage + " (" + m.Name + "." + f + ")\"";
    }

    private void AddAssertEquals(GeneratorConfig.ModelConfig m, GeneratorConfig.ModelField f, string testTarget, string errorMessage)
    {
        liner.Add("Assert.That(" + target + "." + f.Name + ", Is.EqualTo(TestData.Test" + testTarget + ")" + TypeUtils.GetAssertPostfix(f.Type) + "," + FormatErrorMessage(m, f, errorMessage) + ");");
    }
}
