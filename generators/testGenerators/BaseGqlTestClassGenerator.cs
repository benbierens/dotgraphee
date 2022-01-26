
using System;
using System.Collections.Generic;

public class BaseGqlTestClassGenerator : BaseGenerator
{
    public BaseGqlTestClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateBaseGqlTestClass()
    {
        var fm = StartTestUtilsFile("BaseGqlTest");
        AddBaseGqlTestClass(fm.AddClass("BaseGqlTest"));
        AddDockerInitializer(fm.AddClass("DockerInitializer"));
        fm.Build();
    }

    private void AddBaseGqlTestClass(ClassMaker cm)
    {
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");

        cm.AddAttribute("Category(\"" + Config.Tests.TestCategory + "\")");

        cm.AddLine("[SetUp]");
        cm.AddClosure("public async Task GqlSetUp()", liner =>
        {
            liner.Add("TestData = new TestData();");
            liner.Add("await DockerController.Up();");
        });

        cm.AddLine("[TearDown]");
        cm.AddClosure("public async Task GqlTearDown()", liner =>
        {
            liner.Add("await Gql.CloseActiveSubscriptionHandles();");
            liner.Add("DockerController.Down();");
            liner.Add("DockerController.ClearData();");
        });

        cm.AddProperty("TestData")
            .IsType("TestData")
            .Build();

        cm.AddProperty("Gql")
            .IsType("Gql")
            .Build();

        cm.AddBlankLine();
        AddCreateTestModelMethods(cm);
    }

    private void AddCreateTestModelMethods(ClassMaker cm)
    {
        cm.AddUsing(Config.GenerateNamespace);

        IterateModelsInDependencyOrder(m =>
        {
            var foreignProperties = GetForeignProperties(m);
            cm.AddClosure("public async Task<" + m.Name + "> CreateTest" + m.Name + "()", liner =>
            {
                var arguments = new List<string>();
                foreach (var f in foreignProperties)
                {
                    if (!f.IsSelfReference)
                    {
                        liner.Add("await CreateTest" + f.Type + "();");
                        arguments.Add("TestData.Test" + f.Type + ".Id");
                    }
                    else
                    {
                        arguments.Add("null");
                    }
                }

                var args = string.Join(", ", arguments);
                liner.Add("var entity = await Gql.Create" + m.Name + "(TestData.Test" + m.Name + ".ToCreate(" + args + "));");
                liner.Add("TestData.Test" + m.Name + ".Id = entity.Id;");
                liner.Add("return entity;");
            });
        });
    }

    private void AddDockerInitializer(ClassMaker cm)
    {
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