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
    IReadOnlyList<LessonExerciseView> Exercises);

public sealed record LessonExerciseView(
    string Id,
    string Path,
    ExerciseContent Content);

public sealed record ExerciseProgress(
    string ExerciseId,
    bool Passed,
    string LastCode,
    int Attempts,
    DateTimeOffset UpdatedUtc);
