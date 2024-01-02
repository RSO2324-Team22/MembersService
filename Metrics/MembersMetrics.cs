namespace MembersService.Metrics;
using System.Diagnostics.Metrics;

public class MembersMetrics
{
    private readonly Counter<int> _membersAddedCounter;

    public MembersMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Members.Web");
        _membersAddedCounter = meter.CreateCounter<int>("vodopivci.member.added");
    }

    public void MemberAdded(string username)
    {
        _membersAddedCounter.Add(1,
            new KeyValuePair<string, object?>("vodopivci.member.added", username));
    }
}