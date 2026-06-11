namespace ScribanTutorial.Services;

public sealed record Manifest(
    string CourseTitle,
    string CourseSubtitle,
    IReadOnlyList<LessonEntry> Lessons);

public sealed record LessonEntry(
    string Id,
    string Title,
    string TheoryPath,
    IReadOnlyList<ExerciseEntry> Exercises,
    string? Description = null);

public sealed record ExerciseEntry(string Id, string Path, string? Title = null)
{
    // Manifest titles roll out incrementally — fall back to the slug so an
    // entry without one still renders something humane everywhere.
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Id : Title!;
}

// One h2/h3 from a lesson's theory, emitted by ContentBuilder as a
// 01-theory.toc.json sidecar. Drives the in-lesson table of contents.
public sealed record TheoryHeading(string Id, string Text, int Level);

// One hidden validation input for an exercise: an alternative data model and
// the output the canonical solution produces for it. Derived at build time
// from an optional 06-cases.json (array of data-model objects) so authors
// never hand-maintain hidden expecteds. Submissions must pass the visible
// check AND every hidden case — this is what stops learners from hardcoding
// the displayed expected output.
public sealed record ExerciseCase(string DataModelJson, string Expected);

public sealed record ExerciseContent(
    string DescriptionHtml,
    string DataModelJson,
    string DataModelHtml,
    string Expected,
    string StarterTemplate,
    string Solution,
    IReadOnlyList<ExerciseCase> Cases);

public sealed record LessonContent(
    LessonEntry Entry,
    string TheoryHtml,
    IReadOnlyList<LessonExerciseView> Exercises,
    IReadOnlyList<TheoryHeading> TheoryHeadings);

public sealed record LessonExerciseView(
    string Id,
    string Path,
    string Title,
    ExerciseContent Content);

public sealed record ExerciseProgress(
    string ExerciseId,
    bool Passed,
    string LastCode,
    int Attempts,
    DateTimeOffset UpdatedUtc);
