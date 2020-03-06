using System.Collections.Generic;
using System.Linq;
namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class LocalUserProvider : SecurityProvider
    {
        public Dictionary<string, string> users;
        public HashSet<string> admins;
        public LocalUserProvider(SecuritySettings settings)
        {
            this.users = new Dictionary<string, string>();
            this.admins = new HashSet<string>();
            if (settings.AuthUser.HasValue() && settings.AuthPassword.HasValue())
            {
                var accounts = settings.AuthUser.Split(StringSplits.Comma_SemiColon).ToList();
                var passwords = settings.AuthPassword.Split(StringSplits.Comma_SemiColon).ToList();
                for (int i = 0; i < accounts.Count; i++)
                {
                    this.users.Add(accounts[i], passwords[i]);
                }
                if (settings.Admins.HasValue())
                    this.admins = settings.Admins.Split(StringSplits.Comma_SemiColon).ToHashSet();
            }
        }
        public override bool IsAdmin => false;
        internal override bool InReadGroups(ISecurableModule settings)
        {
            return true;
        }
        public override bool InGroups(string groupNames, string accountName)
        {
            return this.admins.Contains(accountName);
        }
        public override bool ValidateUser(string userName, string password)
        {
            return this.users.ContainsKey(userName) ? this.users[userName] == password : false;
        }
    }
}