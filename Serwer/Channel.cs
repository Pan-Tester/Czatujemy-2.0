using System;
using System.Collections.Generic;
using System.Linq;

namespace CzatujiemyServer
{
    public class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Password { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsGlobal { get; set; }
        

        public HashSet<string> ActiveMembers { get; set; } = new HashSet<string>();
        public HashSet<string> AllTimeMembers { get; set; } = new HashSet<string>();
        public Dictionary<string, ChannelRole> MemberRoles { get; set; } = new Dictionary<string, ChannelRole>();
        

        public HashSet<string> BannedUsers { get; set; } = new HashSet<string>();
        public HashSet<string> MutedUsers { get; set; } = new HashSet<string>();
        

        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public Channel()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
        }

        public bool CanUserJoin(string userId, string password = null)
        {
            if (BannedUsers.Contains(userId)) return false;
            if (IsGlobal) return true;
            if (string.IsNullOrEmpty(Password)) return true;
            return Password == password;
        }

        public bool CanUserSpeak(string userId)
        {
            if (BannedUsers.Contains(userId)) return false;
            if (MutedUsers.Contains(userId)) return false;
            return ActiveMembers.Contains(userId);
        }

        public ChannelRole GetUserRole(string userId)
        {
            if (userId == OwnerId) return ChannelRole.Owner;
            return MemberRoles.GetValueOrDefault(userId, ChannelRole.Member);
        }

        public bool CanUserManage(string userId)
        {
            var role = GetUserRole(userId);
            return role == ChannelRole.Owner || role == ChannelRole.Admin;
        }

        public bool CanUserModerate(string userId)
        {
            var role = GetUserRole(userId);
            return role == ChannelRole.Owner || role == ChannelRole.Admin || role == ChannelRole.Moderator;
        }

        public void AddMember(string userId)
        {
            ActiveMembers.Add(userId);
            AllTimeMembers.Add(userId);
            if (!MemberRoles.ContainsKey(userId))
            {
                MemberRoles[userId] = ChannelRole.Member;
            }
        }

        public void RemoveMember(string userId)
        {
            ActiveMembers.Remove(userId);
        }

        public void BanUser(string userId)
        {
            BannedUsers.Add(userId);
            ActiveMembers.Remove(userId);
        }

        public void UnbanUser(string userId)
        {
            BannedUsers.Remove(userId);
        }

        public void MuteUser(string userId)
        {
            MutedUsers.Add(userId);
        }

        public void UnmuteUser(string userId)
        {
            MutedUsers.Remove(userId);
        }
    }

    public enum ChannelRole
    {
        Member,
        Moderator,
        Admin,
        Owner
    }

    public class ChannelInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool HasPassword { get; set; }
        public int MemberCount { get; set; }
        public bool IsGlobal { get; set; }
        public string OwnerId { get; set; }
    }

    public class ChannelMember
    {
        public string UserId { get; set; }
        public string Nick { get; set; }
        public ChannelRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public bool IsMuted { get; set; }
        public DateTime LastSeen { get; set; }
    }
}