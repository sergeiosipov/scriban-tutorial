using System.Runtime.CompilerServices;

namespace ScribanTutorial.Tests;

/// <summary>
/// Resolves on-disk paths from the test runner. We anchor on the
/// <c>ScribanTutorial.slnx</c> at the repo root so the tests work regardless
/// of the test runner's working directory (VS Test Explorer, `dotnet test`,
/// CI, IDE-runner-of-the-week).
/// </summary>
internal static class RepoPaths
{
    public static string RepoRoot { get; } = ResolveRepoRoot();
    public static string LessonsDir => Path.Combine(RepoRoot, "src", "ScribanTutorial", "wwwroot", "lessons");
    public static string ReferenceDir => Path.Combine(RepoRoot, "src", "ScribanTutorial", "wwwroot", "reference");
    public static string ManifestPath => Path.Combine(RepoRoot, "src", "ScribanTutorial", "wwwroot", "manifest.json");
    public static string ScribanGrammarPath => Path.Combine(RepoRoot, "tools", "ContentBuilder", "grammars", "scriban.tmLanguage.json");

    private static string ResolveRepoRoot([CallerFilePath] string thisFile = "")
    {
        var dir = new FileInfo(thisFile).Directory;
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ScribanTutorial.slnx")))
            dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Could not locate ScribanTutorial.slnx walking up from " + thisFile);
        return dir.FullName;
    }
}
