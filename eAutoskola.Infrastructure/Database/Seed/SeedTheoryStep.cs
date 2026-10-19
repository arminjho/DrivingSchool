using eAutoskola.Domain.Entities;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>
/// Tests with questions, study lessons with sections, and the students' solved tests and opened
/// lessons - the real input of the recommender. The demo student's attempts follow the mockups
/// (7/10 on test 1, 9/10, 5/10, 8/10) with the mistakes concentrated in "Saobraćajni znakovi"
/// and "Pravila vožnje" over several attempts, so both recommender signals (error rate and error
/// frequency) have something to work with. Other students get attempts whose correct answers
/// follow a fixed per-student skill, no randomness.
/// </summary>
internal static class SeedTheoryStep
{
    private const int PassThresholdPercent = 70;

    public static async Task RunAsync(EAutoskolaDbContext context, SeedReference reference, SeedPeople people, DateTime now, CancellationToken cancellationToken)
    {
        var studyLessons = SeedStudyLessons(context, reference, now);
        var tests = SeedTests(context, reference);
        await context.SaveChangesAsync(cancellationToken);

        SeedAttempts(context, people, tests, now);
        SeedOpenedLessons(context, people, studyLessons, now);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<StudyLesson> SeedStudyLessons(EAutoskolaDbContext context, SeedReference reference, DateTime now)
    {
        var lessons = new List<StudyLesson>();
        foreach (var seed in SeedTheoryData.StudyLessons)
        {
            var lesson = new StudyLesson
            {
                Title = seed.Title,
                QuestionAreaId = reference.QuestionAreas[seed.AreaIndex].Id,
                Tags = seed.Tags,
                ValidForYear = now.Year,
                ReadingMinutes = seed.ReadingMinutes,
                ChapterLabel = seed.Chapter,
            };

            for (var i = 0; i < seed.Sections.Length; i++)
            {
                lesson.Sections.Add(new StudyLessonSection
                {
                    Title = seed.Sections[i],
                    Content = SeedTheoryData.SectionContent(seed.AreaIndex, seed.Title, seed.Sections[i]),
                    SortOrder = i + 1,
                });
            }

            lessons.Add(lesson);
        }

        context.StudyLessons.AddRange(lessons);
        return lessons;
    }

    private static List<(Test Test, List<Question> Questions)> SeedTests(EAutoskolaDbContext context, SeedReference reference)
    {
        var tests = new List<(Test, List<Question>)>();
        foreach (var seed in SeedTheoryData.Tests)
        {
            var test = new Test
            {
                Title = seed.Title,
                QuestionAreaId = reference.QuestionAreas[seed.AreaIndex].Id,
                PassThresholdPercent = PassThresholdPercent,
            };

            var questions = new List<Question>();
            foreach (var question in seed.Questions)
            {
                var entity = new Question
                {
                    Text = question.Text,
                    QuestionAreaId = reference.QuestionAreas[question.AreaIndex].Id,
                    ImagePath = question.Image is null ? null : SeedImages.Copy(question.Image),
                    Test = test,
                    AnswerOptions = question.Options.Select((text, i) => new AnswerOption { Text = text, IsCorrect = i == question.Correct }).ToList(),
                };
                questions.Add(entity);
            }

            context.Tests.Add(test);
            context.Questions.AddRange(questions);
            tests.Add((test, questions));
        }

        return tests;
    }

    private static void SeedAttempts(EAutoskolaDbContext context, SeedPeople people, List<(Test Test, List<Question> Questions)> tests, DateTime now)
    {
        // The demo student: (test index, days ago, indexes of the questions answered WRONG).
        (int Test, int DaysAgo, int[] Wrong)[] demo =
        [
            (0, 24, [1, 2, 4, 6, 8, 9]),
            (1, 15, [6]),
            (2, 9, [0, 2, 4, 6, 8]),
            (3, 6, [3, 9]),
            (0, 3, [1, 5, 8]),
        ];
        foreach (var (testIndex, daysAgo, wrong) in demo)
        {
            AddAttempt(context, people.Students[0], tests[testIndex], now.AddDays(-daysAgo), wrong);
        }

        // Everyone else who is active and has been around: 1-3 attempts at a fixed skill level.
        var established = people.Students.Length - SeedFleetAndPeopleData.NewStudentCount;
        for (var s = 1; s < established; s++)
        {
            var student = people.Students[s];
            if (!student.IsActive)
            {
                continue;
            }

            var skill = 40 + s * 17 % 55;
            var attempts = 1 + s % 3;
            for (var k = 0; k < attempts; k++)
            {
                var testIndex = (s + k * 2) % tests.Count;
                var wrong = Enumerable.Range(0, tests[testIndex].Questions.Count)
                    .Where(q => (s * 31 + testIndex * 17 + q * 13 + k * 7) % 100 >= skill)
                    .ToArray();
                AddAttempt(context, student, tests[testIndex], now.AddDays(-(2 + s * 3 + k * 5)), wrong);
            }
        }
    }

    private static void AddAttempt(EAutoskolaDbContext context, User student, (Test Test, List<Question> Questions) test, DateTime submittedAt, int[] wrongIndexes)
    {
        var total = test.Questions.Count;
        var incorrect = wrongIndexes.Length;
        var percent = Math.Round(100.0 * (total - incorrect) / total, 1);

        var attempt = new TestAttempt
        {
            Student = student,
            Test = test.Test,
            StartedAt = submittedAt.AddMinutes(-12),
            SubmittedAt = submittedAt,
            CorrectCount = total - incorrect,
            IncorrectCount = incorrect,
            Percent = percent,
            Passed = percent >= PassThresholdPercent,
        };

        for (var q = 0; q < total; q++)
        {
            var question = test.Questions[q];
            var isWrong = wrongIndexes.Contains(q);
            var selected = isWrong
                ? question.AnswerOptions.First(o => !o.IsCorrect)
                : question.AnswerOptions.First(o => o.IsCorrect);

            attempt.Answers.Add(new AttemptAnswer { Question = question, SelectedAnswerOption = selected, IsCorrect = !isWrong });
        }

        context.TestAttempts.Add(attempt);
    }

    /// <summary>"Moje lekcije": the lessons each student has opened. The demo student has opened the two
    /// the mockup (4.2.9) shows.</summary>
    private static void SeedOpenedLessons(EAutoskolaDbContext context, SeedPeople people, List<StudyLesson> lessons, DateTime now)
    {
        void Open(User student, StudyLesson lesson, int daysAgo)
            => context.StudentStudyLessons.Add(new StudentStudyLesson
            {
                Student = student, StudyLesson = lesson,
                OpenedAt = now.AddDays(-daysAgo - 2), LastOpenedAt = now.AddDays(-daysAgo), ProgressPercent = 100,
            });

        Open(people.Students[0], lessons[0], 3);
        Open(people.Students[0], lessons[4], 8);

        for (var s = 1; s <= 12; s++)
        {
            var first = s % lessons.Count;
            var second = s * 3 % lessons.Count;
            Open(people.Students[s], lessons[first], s);
            if (second != first)
            {
                Open(people.Students[s], lessons[second], s + 4);
            }
        }
    }
}
