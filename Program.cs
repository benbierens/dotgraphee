using System;

namespace generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigLoader().Get(args);
            var validator = new ConfigValidator();
            validator.Validate(config);

            if (!validator.IsValid)
            {
                Console.WriteLine("Configuration errors:");
                foreach (var error in validator.Errors)
                {
                    Console.WriteLine(error);
                }
                return;
            }

            var gen = new Generator(config);
            gen.Generate();
        }
    }
}
