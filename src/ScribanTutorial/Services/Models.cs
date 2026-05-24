namespace ScribanTutorial.Services;

public sealed record Manifest(
    string CourseTitle,
    string CourseSubtitle,
    IReadOnlyList<LessonEntry> Lessons);

public sealed record LessonEntry(
    string Id,
    string Title,
    string TheoryPath,
    IReadOnlyList<ExerciseEntry> Exercises);

public sealed record ExerciseEntry(string Id, string Path);

public sealed record ExerciseContent(
    string DescriptionHtml,
    string DataModelJson,
    string DataModelHtml,
    string Expected,
    string StarterTemplate,
    string Solution);

public sealed record LessonContent(
    LessonEntry Entry,
    string TheoryHtml,
    IReadOnlyDictionary<string, ExerciseContent> Exercises);

public sealed record ExerciseProgress(
    string ExerciseId,
    bool Passed,
    string LastCode,
    int Attempts,
    DateTimeOffset UpdatedUtc);
