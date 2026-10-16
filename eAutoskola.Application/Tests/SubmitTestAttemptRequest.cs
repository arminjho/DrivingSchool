namespace eAutoskola.Application.Tests;

public class SubmittedAnswer
{
    public int QuestionId { get; set; }
    public int? SelectedAnswerOptionId { get; set; }
}

public class SubmitTestAttemptRequest
{
    public List<SubmittedAnswer> Answers { get; set; } = [];
}
