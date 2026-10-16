namespace eAutoskola.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public string? ImagePath { get; set; }

    public int QuestionAreaId { get; set; }
    public QuestionArea? QuestionArea { get; set; }

    public int TestId { get; set; }
    public Test? Test { get; set; }

    public ICollection<AnswerOption> AnswerOptions { get; set; } = [];
}
