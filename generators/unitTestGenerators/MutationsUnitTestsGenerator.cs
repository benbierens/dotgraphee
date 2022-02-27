using System;

public class MutationsUnitTestsGenerator : BaseUnitTestGenerator
{
    private readonly string mutationsName;

    public MutationsUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
        mutationsName = config.Config.GraphQl.GqlMutationsClassName.FirstToLower();
    }

    public void GenerateMutationUnitTests()
    {
        var fm = StartUnitTestFile("Mutations", Config.Output.GraphQlSubFolder);
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing("HotChocolate");
        fm.AddUsing("HotChocolate.Subscriptions");
        fm.AddUsing("Moq");
        fm.AddUsing("System");
        fm.AddUsing("System.Threading.Tasks");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("MutationTests");
        cm.AddInherrit("BaseUnitTest");
        cm.Modifiers.Clear();
        cm.AddAttribute("TestFixture");

        cm.AddLine("private " + Config.GraphQl.GqlMutationsClassName + " " + mutationsName + " = null!;");
        cm.AddLine("private Mock<ITopicEventSender> sender = null!;");
        cm.AddLine("private Mock<IPublisher> publisher = null!;");
        cm.AddLine("private Mock<IInputConverter> inputConverter = null!;");

        cm.AddBlankLine();
        cm.AddLine("[SetUp]");
        cm.AddClosure("public void SetUp()", liner =>
        {
            liner.Add("sender = new Mock<ITopicEventSender>();");
            liner.Add("publisher = new Mock<IPublisher>();");
            liner.Add("inputConverter = new Mock<IInputConverter>();");
            liner.AddBlankLine();
            liner.Add(mutationsName + " = new " + Config.GraphQl.GqlMutationsClassName + "(" + GetDbAccessorName() + ".Object, publisher.Object, inputConverter.Object);");
            liner.AddBlankLine();
            
            foreach (var m in Models)
            {
                var inputTypes = GetInputTypeNames(m);
                liner.Add("inputConverter.Setup(i => i.ToDto(TestData." + inputTypes.Create + ")).Returns(TestData." + m.Name + "1);");
            }
        });

        foreach (var m in Models)
        {
            cm.BeginRegion(m.Name);

            var inputTypes = GetInputTypeNames(m);
            AddCreateTests(cm, m, inputTypes);
            AddUpdateTests(cm, m, inputTypes);
            AddDeleteTests(cm, m, inputTypes);

            cm.EndRegion();
        }

        fm.Build();
    }

    private void AddCreateTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
        if (IsRequiredSubModel(m)) return;
        var method = Config.GraphQl.GqlMutationsCreateMethod;
        var methodName = method + m.Name;

        AddAsyncTest(cm, methodName + "ShouldConvertInput", liner =>
        {
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("inputConverter.Verify(i => i.ToDto(TestData." + inputTypes.Create + "));");
        });

        AddAsyncTest(cm, methodName + "ShouldAdd" + m.Name, liner =>
        {
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("DbService.Verify(db => db.Add(TestData." + m.Name + "1));");
        });

        var pub = "Publish" + m.Name + Config.GraphQl.GqlSubscriptionCreatedMethod;
        AddAsyncTest(cm, methodName + "Should" + pub, liner =>
        {
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("publisher.Verify(p => p." + pub + "(sender.Object, TestData." + m.Name + "1));");
        });

        AddAsyncTest(cm, methodName + "ShouldReturn" + m.Name + "Queryable", liner =>
        {
            liner.Add("var expectedResult = MockDbServiceQueryableEntity(TestData." + m.Name + "1);");
            liner.AddBlankLine();
            liner.Add("var result = await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("AssertQueryableAreEqual(expectedResult.Object, result);");
        });
    }

    private void AddUpdateTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
        var method = Config.GraphQl.GqlMutationsUpdateMethod;
        var methodName = method + m.Name;

        AddAsyncTest(cm, methodName + "ShouldCallUpdate", liner =>
        {
            AddMockDatabaseUpdateReturnValue(liner, m);
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("DbService.Verify(db => db.Update(TestData." + inputTypes.Update + "." + m.Name + "Id, It.IsAny<Action<" + m.Name + ">>()));");
        });

        AddAsyncTest(cm, methodName + "Should" + methodName, liner =>
        {
            liner.Add("DbService.Setup(db => db.Update(It.IsAny<int>(), It.IsAny<Action<" + m.Name + ">>())).Callback(");
            liner.Indent();
            liner.StartClosure("new Action<int, Action<" + m.Name + ">>((id, updateAction) =>");
            liner.Add("updateAction(TestData." + m.Name + "1);");
            liner.EndClosure(")).Returns(TestData." + m.Name + "1);");
            liner.Deindent();
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object);");
            liner.AddBlankLine();
            AddAssertUpdateFields(liner, m, inputTypes);
        });

        AddUpdateFailedToFindTest(cm, m, method, methodName, inputTypes);

        var pub = "Publish" + m.Name + Config.GraphQl.GqlSubscriptionUpdatedMethod;
        AddAsyncTest(cm, methodName + "Should" + pub, liner =>
        {
            AddMockDatabaseUpdateReturnValue(liner, m);
            liner.Add("await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("publisher.Verify(p => p." + pub + "(sender.Object, TestData." + m.Name + "1));");
        });

        AddAsyncTest(cm, methodName + "ShouldReturn" + m.Name + "Queryable", liner =>
        {
            liner.Add("var expectedResult = MockDbServiceQueryableEntity(TestData." + m.Name + "1);");
            AddMockDatabaseUpdateReturnValue(liner, m);
            liner.Add("var result = await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("AssertQueryableAreEqual(expectedResult.Object, result);");
        });
    }

    private void AddAssertUpdateFields(Liner liner, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
        foreach (var field in m.Fields)
        {
            AddAssertUpdateEqualsLine(liner, m, field.Name, inputTypes);
        }
        var foreignProperties = GetForeignProperties(m);
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
            {
                AddAssertUpdateEqualsLine(liner, m, f.WithId, inputTypes);
            }
        }
        var optionalSubModels = GetMyOptionalSubModels(m);
        foreach (var subModel in optionalSubModels)
        {
            AddAssertUpdateEqualsLine(liner, m, subModel.Name + "Id", inputTypes);
        }
    }

    private void AddAssertUpdateEqualsLine(Liner liner, GeneratorConfig.ModelConfig m, string fieldName, InputTypeNames inputTypes)
    {
        liner.Add("Assert.That(TestData." + m.Name + "1." + fieldName + ", Is.EqualTo(TestData." + inputTypes.Update + "." + fieldName + "));");
    }

    private void AddMockDatabaseUpdateReturnValue(Liner liner, GeneratorConfig.ModelConfig m)
    {
        liner.Add("DbService.Setup(db => db.Update(It.IsAny<int>(), It.IsAny<Action<" + m.Name + ">>())).Returns(TestData." + m.Name + "1);");
        liner.AddBlankLine();
    }

    private void AddUpdateFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m, string method, string methodName, InputTypeNames inputTypes)
    {
        if (IsFailedToFindStrategyErrorCode())
        {
            AddTest(cm, methodName + "ShouldThrowIfNotFound", liner =>
            {
                liner.Add("Assert.That(async () => await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object),");
                liner.Indent();
                liner.Add("Throws.TypeOf<GraphQLException>());");
                liner.Deindent();
            });
        }
        else if (IsFailedToFindStrategyNullObject())
        {
            AddAsyncTest(cm, methodName + "ShouldReturnNullIfNotFound", liner =>
            {
                liner.Add("var result = await " + mutationsName + "." + methodName + "(TestData." + inputTypes.Update + ", sender.Object);");
                liner.Add("Assert.That(result, Is.Null);");
            });
        }
        else
        {
            throw new Exception("Unknown FailedToFind strategy");
        }
    }

    private void AddDeleteTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
    }
}
