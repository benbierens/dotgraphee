using System.Collections.Generic;
using System.Linq;

public class BaseGqlTestClassGenerator : BaseTestGenerator
{
    public BaseGqlTestClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateBaseGqlTestClass()
    {
        var fm = StartIntegrationTestUtilsFile("BaseGqlTest");
        fm.AddUsing(Config.Output.UnitTestFolder.FirstToUpper());
        AddBaseGqlTestClass(fm.AddClass("BaseGqlTest"));
        AddDockerInitializer(fm.AddClass("DockerInitializer"));
        fm.Build();
    }

    private void AddBaseGqlTestClass(ClassMaker cm)
    {
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("StrawberryShake");
        cm.AddUsing(Config.GenerateNamespace + ".Client");

        cm.AddAttribute("Category(\"" + Config.IntegrationTests.TestCategory + "\")");

        cm.AddLine("[SetUp]");
        cm.AddClosure("public async Task GqlSetUp()", liner =>
        {
            liner.Add("TestData = new TestData();");
            liner.Add("TestInput = new TestInput(TestData);");
            liner.Add("await DockerController.Up();");
        });

        cm.AddLine("[TearDown]");
        cm.AddClosure("public void GqlTearDown()", liner =>
        {
            liner.Add("Gql.CloseActiveSubscriptionHandles();");
            liner.Add("DockerController.ClearData();");
            liner.Add("DockerController.Restart();");
        });

        cm.AddProperty("TestData")
            .IsType("TestData")
            .InitializeAsExplicitNull()
            .Build();

        cm.AddProperty("TestInput")
            .IsType("TestInput")
            .InitializeAsExplicitNull()
            .Build();

        cm.AddProperty("Gql")
            .IsType("Gql")
            .Build();

        cm.AddBlankLine();
        AddCreateTestModelMethods(cm);

        AddAssertNoErrors(cm);
    }

    private void AddAssertNoErrors(ClassMaker cm)
    {
        cm.AddClosure("public void AssertNoErrors(IOperationResult gqlData)", liner =>
        {
            liner.Add("gqlData.EnsureNoErrors();");
        });
    }

    private void AddCreateTestModelMethods(ClassMaker cm)
    {
        IterateModelsInDependencyOrder(m =>
        {
            if (!IsRequiredSubModel(m))
            {
                AddCreateTestModelMethod(cm, m);
            }
        });
    }

    private void AddCreateTestModelMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);
        var methodName = Config.GraphQl.GqlMutationsCreateMethod + m.Name;
        var returnType = "I" + methodName + "_" + methodName;

        cm.AddClosure("public async Task<" + returnType + "> CreateTest" + m.Name + "()", liner =>
        {
            liner.Add("var gqlData = await Gql.Create" + m.Name + "(TestInput.To" + inputTypes.Create + "());");
            AddAssert(liner).NoErrors();
            liner.Add("var entity = gqlData.Data!." + methodName + ";");
            AddAssert(liner).EntityNotNull("CreateTest" + m.Name);
            AddAssignIdToTestData(liner, m, "entity");
            liner.Add("return entity;");
        });
    }

    private void AddAssignIdToTestData(Liner liner, GeneratorConfig.ModelConfig m, params string[] accessors)
    {
        var accessor = string.Join(".", accessors) + ".Id";
        liner.Add("TestData." + m.Name + "1.Id = " + accessor + ";");

        var subModels = GetMyRequiredSubModels(m);
        foreach (var subModel in subModels)
        {
            AddAssignIdToTestData(liner, subModel, accessors.Concat(new[] { subModel.Name }).ToArray());
        }
    }

    private void AddDockerInitializer(ClassMaker cm)
    {
        cm.AddAttribute("Category(\"" + Config.IntegrationTests.TestCategory + "\")");
        cm.AddAttribute("SetUpFixture");

        cm.AddLine("[OneTimeSetUp]");
        cm.AddClosure("public async Task OneTimeGqlSetUp()", liner =>
        {
            liner.Add("await DockerController.BuildImage();");
        });

        cm.AddLine("[OneTimeTearDown]");
        cm.AddClosure("public void OneTimeGqlTearDown()", liner =>
        {
            liner.Add("DockerController.DeleteImage();");
        });
    }
}