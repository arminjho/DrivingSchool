using eAutoskola.Application.Recommendations;

namespace eAutoskola.Tests.Recommendations;

public class StudentErrorProfileBuilderTests
{
    [Fact]
    public void Build_NoAnsweredQuestions_ReturnsEmptyProfile()
    {
        var profile = StudentErrorProfileBuilder.Build([]);

        Assert.False(profile.HasAnyData);
        Assert.Empty(profile.Weights);
    }

    [Fact]
    public void Build_AllCorrect_ProducesZeroWeightForThatArea()
    {
        var answers = new[]
        {
            new AnsweredQuestionRecord(QuestionAreaId: 1, TestAttemptId: 100, IsCorrect: true),
            new AnsweredQuestionRecord(QuestionAreaId: 1, TestAttemptId: 100, IsCorrect: true),
        };

        var profile = StudentErrorProfileBuilder.Build(answers);

        Assert.True(profile.HasAnyData);
        Assert.Equal(0.0, profile.Weights[1]);
        Assert.False(profile.AreaStats[1].IsCritical);
    }

    [Fact]
    public void Build_ErrorsAcrossMultipleAttempts_MarksAreaCritical()
    {
        // 2 distinct attempts each with >=1 error in area 1 -> ErrorFrequency = 2 = threshold.
        var answers = new[]
        {
            new AnsweredQuestionRecord(1, TestAttemptId: 1, IsCorrect: false),
            new AnsweredQuestionRecord(1, TestAttemptId: 2, IsCorrect: false),
        };

        var profile = StudentErrorProfileBuilder.Build(answers);

        Assert.Equal(RecommenderConstants.CriticalErrorFrequencyThreshold, profile.AreaStats[1].ErrorFrequency);
        Assert.True(profile.AreaStats[1].IsCritical);
    }

    [Fact]
    public void Build_SingleAttemptWithErrors_IsNotCriticalBelowThreshold()
    {
        var answers = new[]
        {
            new AnsweredQuestionRecord(1, TestAttemptId: 1, IsCorrect: false),
            new AnsweredQuestionRecord(1, TestAttemptId: 1, IsCorrect: false),
        };

        var profile = StudentErrorProfileBuilder.Build(answers);

        Assert.Equal(1, profile.AreaStats[1].ErrorFrequency);
        Assert.False(profile.AreaStats[1].IsCritical);
        // ErrorRate signal alone must still produce a non-zero weight - no signal is collected
        // and then ignored (upute.md §2.4).
        Assert.True(profile.Weights[1] > 0);
    }

    [Fact]
    public void Build_BothSignalsInfluenceWeight_HigherFrequencyAreaScoresHigherAtEqualErrorRate()
    {
        // Area 1: 1 wrong out of 2, in a single attempt (ErrorRate 0.5, ErrorFrequency 1).
        // Area 2: 2 wrong out of 4 (same ErrorRate 0.5), but spread across 2 attempts
        //         (ErrorFrequency 2) - frequency alone differs, so weight must differ too,
        //         proving ErrorFrequency genuinely contributes to the score.
        var answers = new[]
        {
            new AnsweredQuestionRecord(1, TestAttemptId: 1, IsCorrect: false),
            new AnsweredQuestionRecord(1, TestAttemptId: 1, IsCorrect: true),
            new AnsweredQuestionRecord(2, TestAttemptId: 2, IsCorrect: false),
            new AnsweredQuestionRecord(2, TestAttemptId: 2, IsCorrect: true),
            new AnsweredQuestionRecord(2, TestAttemptId: 3, IsCorrect: false),
            new AnsweredQuestionRecord(2, TestAttemptId: 3, IsCorrect: true),
        };

        var profile = StudentErrorProfileBuilder.Build(answers);

        Assert.Equal(profile.AreaStats[1].ErrorRate, profile.AreaStats[2].ErrorRate);
        Assert.True(profile.Weights[2] > profile.Weights[1]);
    }
}
