using System.Collections.Generic;
using System.Linq;
using Opserver;
using Opserver.Models;
using Opserver.Security;

namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class LocalUserProvider : SecurityProvider
    {
        public Dictionary<string, string> users;
        public HashSet<string> admins;
        public override string ProviderName => "Local User Provider";

        public LocalUserProvider(SecuritySettings settings) : base(settings)
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
                {
                    admins = Enumerable.ToHashSet(settings.Admins.Split(StringSplits.Comma_SemiColon));
                }
            }
        }

        internal override bool InReadGroups(User user, StatusModule module)
        {
            return true;
        }

        public override bool InGroups(User user, string groupNames)
        {
            return this.admins.Contains(user.AccountName);
        }

        public override bool ValidateUser(string userName, string password)
        {
            return this.users.ContainsKey(userName) ? this.users[userName] == password : false;
        }
    }
}
