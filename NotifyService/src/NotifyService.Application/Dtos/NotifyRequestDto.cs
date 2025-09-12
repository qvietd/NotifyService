namespace NotifyService.Application.Dtos;

public class NotifyMessageTestDto
{
    public string MessageId { get; set; }  = Guid.NewGuid().ToString();

    // userId
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Type { get; set; }
}