using System.Collections.Generic;
using UnityEngine;

public interface IWildlifeGroupMember
{
    Transform WildlifeTransform { get; }
    bool IsAvailableForWildlifeGroup { get; }
    void JoinWildlifeGroup(WildlifeGroup group);
    void LeaveWildlifeGroup(WildlifeGroup group);
}

public sealed class WildlifeGroup
{
    private readonly List<IWildlifeGroupMember> members = new List<IWildlifeGroupMember>();

    public int GroupId { get; }
    public int MemberCount => members.Count;

    public WildlifeGroup(int groupId)
    {
        GroupId = groupId;
    }

    public void AddMember(IWildlifeGroupMember member)
    {
        if (member == null || members.Contains(member))
            return;

        members.Add(member);
        member.JoinWildlifeGroup(this);
    }

    public void RemoveMember(IWildlifeGroupMember member)
    {
        if (member == null)
            return;

        if (members.Remove(member))
            member.LeaveWildlifeGroup(this);
    }

    public bool TryGetCenter(IWildlifeGroupMember requester, out Vector2 center)
    {
        center = default;

        Vector2 sum = Vector2.zero;
        int count = 0;

        for (int i = members.Count - 1; i >= 0; i--)
        {
            IWildlifeGroupMember member = members[i];
            if (member == null || member.WildlifeTransform == null)
            {
                members.RemoveAt(i);
                continue;
            }

            if (!member.IsAvailableForWildlifeGroup)
                continue;

            sum += (Vector2)member.WildlifeTransform.position;
            count++;
        }

        if (requester != null && count <= 1)
            return false;

        if (count <= 0)
            return false;

        center = sum / count;
        return true;
    }
}
