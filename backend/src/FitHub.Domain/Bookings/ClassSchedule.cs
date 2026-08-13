using FitHub.Domain.Common;using FitHub.Domain.Identity;
namespace FitHub.Domain.Bookings;
public sealed class ClassSchedule:Entity
{
    private ClassSchedule(){}
    public ClassSchedule(string name,string? description,DateTimeOffset startsAt,DateTimeOffset endsAt,int capacity,Guid instructorUserId){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Class name is required.");if(endsAt<=startsAt)throw new ArgumentException("Class end must be after its start.");if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));Name=name.Trim();Description=string.IsNullOrWhiteSpace(description)?null:description.Trim();StartsAt=startsAt;EndsAt=endsAt;Capacity=capacity;InstructorUserId=instructorUserId;}
    public string Name{get;private set;}=string.Empty;public string? Description{get;private set;}public DateTimeOffset StartsAt{get;private set;}public DateTimeOffset EndsAt{get;private set;}public int Capacity{get;private set;}public Guid? InstructorUserId{get;private set;}public ApplicationUser? InstructorUser{get;private set;}public ICollection<ClassBooking> Bookings{get;}=[];
}
