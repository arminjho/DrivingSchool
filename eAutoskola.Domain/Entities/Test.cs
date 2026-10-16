namespace eAutoskola.Domain.Entities;

public class Test
{
    public int Id { get; set; }
    public required string Title { get; set; }

    public int QuestionAreaId { get; set; }
    public QuestionArea? QuestionArea { get; set; }

    public int PassThresholdPercent { get; set; } = 70;

    public ICollection<Question> Questions { get; set; } = [];
}
