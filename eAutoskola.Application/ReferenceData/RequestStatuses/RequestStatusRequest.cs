namespace eAutoskola.Application.ReferenceData.RequestStatuses;

public class RequestStatusRequest
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
