using FitHub.Domain.Common;using FitHub.Domain.Identity;
namespace FitHub.Domain.Communications;
public sealed class Recommendation:Entity
{
    private Recommendation(){}
    public Recommendation(Guid userId,string ruleCode,string title,string content,string category,string reason,DateTimeOffset? expiresAt=null){UserId=userId;RuleCode=Required(ruleCode,nameof(ruleCode));Title=Required(title,nameof(title));Content=Required(content,nameof(content));Category=Required(category,nameof(category));Reason=Required(reason,nameof(reason));ExpiresAt=expiresAt;}
    public Guid UserId{get;private set;}public ApplicationUser User{get;private set;}=null!;
    public string RuleCode{get;private set;}=string.Empty;public string Title{get;private set;}=string.Empty;public string Content{get;private set;}=string.Empty;public string Category{get;private set;}=string.Empty;public string Reason{get;private set;}=string.Empty;
    public RecommendationStatus Status{get;private set;}=RecommendationStatus.Active;public DateTimeOffset? ExpiresAt{get;private set;}
    public void Expire(DateTimeOffset at){Status=RecommendationStatus.Expired;ExpiresAt=at;MarkUpdated(at);}
    private static string Required(string value,string name)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException($"{name} is required.",name):value.Trim();
}
