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
        if (!string.Equals(actual, want, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): output differs from expected.");
            Console.Error.WriteLine("--- expected ---");
            Console.Error.WriteLine(want);
            Console.Error.WriteLine("--- actual ---");
            Console.Error.WriteLine(actual);
            Console.Error.WriteLine("----------------");
            return 1;
        }

        // Hidden validation cases (optional 06-cases.json): their expected
        // outputs are DERIVED from this very solution at bundle time, so there
        // is nothing stored to compare against — derivation IS the contract.
        // The check --verify owns is that the solution renders cleanly against
        // every case.
        int? caseCount = null;
        var casesPath = Path.Combine(exerciseDir, "06-cases.json");
        if (File.Exists(casesPath))
        {
            var caseModels = CliApp.TryLoadCaseModels(casesPath, out var caseError);
            if (caseModels is null)
            {
                Console.Error.WriteLine($"--verify: {caseError}");
                return 2;
            }
            for (var i = 0; i < caseModels.Count; i++)
            {
                var caseResult = ScribanRunner.Run(solution, caseModels[i]);
                if (!caseResult.Ok)
                {
                    Console.Error.WriteLine(
                        $"--verify FAIL ({exerciseDir}): case {i} of 06-cases.json failed to render — {caseResult.Errors}");
                    return 1;
                }
            }
            caseCount = caseModels.Count;
        }

        Console.WriteLine($"--verify OK ({exerciseDir})");
        if (caseCount is not null)
            Console.WriteLine($"cases: {caseCount} rendered OK");
        return 0;
    }
}
