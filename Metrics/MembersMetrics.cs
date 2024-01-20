using System.Diagnostics.Metrics;

namespace MembersService.Metrics;

public class MembersMetrics
{
    private readonly Counter<int> _membersAddedCounter;

    public MembersMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Members.Web");
        _membersAddedCounter = meter.CreateCounter<int>("member.added");
    }

    public void MemberAdded(string username)
    {
        _membersAddedCounter.Add(1,
            new KeyValuePair<string, object?>("member.added", username));
    }
}
