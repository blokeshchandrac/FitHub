using FitHub.Domain.Common;using FitHub.Domain.Identity;
namespace FitHub.Domain.Communications;
public sealed class Notification:Entity
{
    private Notification(){}
    public Notification(Guid userId,NotificationType type,string title,string message,string deduplicationKey){UserId=userId;Type=type;Title=string.IsNullOrWhiteSpace(title)?throw new ArgumentException("Title is required."):title.Trim();Message=string.IsNullOrWhiteSpace(message)?throw new ArgumentException("Message is required."):message.Trim();DeduplicationKey=string.IsNullOrWhiteSpace(deduplicationKey)?throw new ArgumentException("Deduplication key is required."):deduplicationKey;}
    public Guid UserId{get;private set;}public ApplicationUser User{get;private set;}=null!;public string Title{get;private set;}=string.Empty;public string Message{get;private set;}=string.Empty;public NotificationType Type{get;private set;}public string DeduplicationKey{get;private set;}=string.Empty;public bool IsRead{get;private set;}public DateTimeOffset? ReadAt{get;private set;}
    public void MarkRead(DateTimeOffset at){if(IsRead)return;IsRead=true;ReadAt=at;MarkUpdated(at);}
}
