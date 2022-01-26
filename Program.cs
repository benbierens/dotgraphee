
namespace generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigLoader().Get(args);
            var gen = new Generator(config);

            gen.Generate();
        }
    }
}
