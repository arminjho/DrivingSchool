using eAutoskola.Application.Recommendations;

namespace eAutoskola.Tests.Recommendations;

public class StudyLessonVectorBuilderTests
{
    private static readonly Dictionary<int, string> AreaNames = new()
    {
        [1] = "Saobraćajni znakovi",
        [2] = "Pravila vožnje",
        [3] = "Prva pomoć",
    };

    [Fact]
    public void Build_PrimaryArea_GetsFullWeight()
    {
        var lesson = new StudyLessonMetadata(StudyLessonId: 10, PrimaryQuestionAreaId: 1, Tags: "znakovi, obaveza");

        var vector = StudyLessonVectorBuilder.Build(lesson, AreaNames);

        Assert.Equal(1.0, vector[1]);
    }

    [Fact]
    public void Build_TagsMentioningAnotherArea_AddsSecondaryWeight()
    {
        var lesson = new StudyLessonMetadata(10, PrimaryQuestionAreaId: 1, Tags: "znakovi, povezano sa Pravila vožnje");

        var vector = StudyLessonVectorBuilder.Build(lesson, AreaNames);

        Assert.Equal(RecommenderConstants.TagAssociationWeight, vector[2]);
    }

    [Fact]
    public void Build_TagsWithoutOtherAreaMentions_OnlyHasPrimaryDimension()
    {
        var lesson = new StudyLessonMetadata(10, PrimaryQuestionAreaId: 1, Tags: "opasnost, zabrana");

        var vector = StudyLessonVectorBuilder.Build(lesson, AreaNames);

        Assert.Single(vector);
        Assert.True(vector.ContainsKey(1));
    }

    [Fact]
    public void Build_TagMatchIsCaseInsensitive()
    {
        var lesson = new StudyLessonMetadata(10, PrimaryQuestionAreaId: 1, Tags: "vezano za PRVA POMOĆ");

        var vector = StudyLessonVectorBuilder.Build(lesson, AreaNames);

        Assert.True(vector.ContainsKey(3));
    }
}
