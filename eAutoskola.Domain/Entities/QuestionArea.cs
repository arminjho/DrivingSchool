namespace eAutoskola.Domain.Entities;

/// <summary>
/// CRUD now (prompt 2); consumed by the Test module (Question.QuestionAreaId) in a later phase.
/// </summary>
public class QuestionArea
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
