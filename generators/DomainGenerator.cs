public class DomainGenerator : BaseGenerator
{
    private readonly DtoGenerator dtoGenerator;
    private readonly GraphQlTypesGenerator gqlTypesGenerator;

    public DomainGenerator(GeneratorConfig config)
        : base(config)
    {
        dtoGenerator = new DtoGenerator(config);
        gqlTypesGenerator = new GraphQlTypesGenerator(config);
    }

    public void GenerateDomain()
    {
        dtoGenerator.GenerateDtos();
        gqlTypesGenerator.GenerateGraphQlTypes();
    }
}