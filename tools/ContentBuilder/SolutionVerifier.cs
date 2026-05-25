using ScribanTutorial.Services;

namespace ContentBuilder;

internal static class SolutionVerifier
{
    public static int Verify(string exerciseDir)
    {
        if (!Directory.Exists(exerciseDir))
        {
            Console.Error.WriteLine($"--verify: directory not found: {exerciseDir}");
            return 2;
        }

        var datamodelPath = Path.Combine(exerciseDir, "02-datamodel.json");
        var expectedPath  = Path.Combine(exerciseDir, "03-expected.txt");
        var solutionPath  = Path.Combine(exerciseDir, "05-solution.txt");

        foreach (var p in new[] { datamodelPath, expectedPath, solutionPath })
        {
            if (!File.Exists(p))
            {
                Console.Error.WriteLine($"--verify: required file missing: {p}");
                return 2;
            }
        }

        var datamodel = File.ReadAllText(datamodelPath);
        var expected  = File.ReadAllText(expectedPath);
        var solution  = File.ReadAllText(solutionPath);

        var result = ScribanRunner.Run(solution, datamodel);
        if (!result.Ok)
        {
            Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): {result.Errors}");
            return 1;
        }

        var actual = ContentNormalize.Normalize(result.Output);
        var want   = ContentNormalize.Normalize(expected);
        if (string.Equals(actual, want, StringComparison.Ordinal))
        {
            Console.WriteLine($"--verify OK ({exerciseDir})");
            return 0;
        }

        Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): output differs from expected.");
        Console.Error.WriteLine("--- expected ---");
        Console.Error.WriteLine(want);
        Console.Error.WriteLine("--- actual ---");
        Console.Error.WriteLine(actual);
        Console.Error.WriteLine("----------------");
        return 1;
    }
}
