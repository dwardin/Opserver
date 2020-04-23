using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Models.Security;

namespace Opserver.Security
{
    public class SecurityManager
    {
        public SecurityProvider CurrentProvider { get; }

        public SecurityManager(IOptions<SecuritySettings> settings, IMemoryCache cache)
        {
            _ = settings?.Value ??
                throw new ArgumentNullException(nameof(settings), "SecuritySettings must be provided");
            CurrentProvider = GetProvider(settings.Value, cache);
        }

        private SecurityProvider GetProvider(SecuritySettings settings, IMemoryCache cache) =>
            settings.Provider switch
            {
                "AD" => new ActiveDirectoryProvider(settings, cache),
                "ActiveDirectory" => new ActiveDirectoryProvider(settings, cache),
                "EveryonesAnAdmin" => new EveryonesAnAdminProvider(settings),
                "local" => new LocalUserProvider(settings),

                //case "EveryonesReadOnly":
                _ => new EveryonesReadOnlyProvider(settings),
            };
    }
}
