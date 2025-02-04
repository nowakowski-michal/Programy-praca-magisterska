

using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Memcached_app.Models
{
    public class AppDbContext
    {
        public static IMemcachedClient MemcachedClient { get; private set; }

        static AppDbContext()
        {
            // Konfiguracja i uruchomienie połączenia z Memcached przy użyciu ServiceCollection
            var serviceProvider = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {    
                })
                .AddEnyimMemcached(options =>
                {
                    options.Servers.Add(new Server { Address = "127.0.0.1", Port = 11211 }); 
                })
                .BuildServiceProvider();

            // Pobranie klienta Memcached z kontenera usług
            MemcachedClient = serviceProvider.GetService<IMemcachedClient>();
        }
   
        public static List<string> GetKeysByCategory(string category)
        {
            var keys = MemcachedClient.Get<List<string>>($"{category}_keys");
            return keys ?? new List<string>();
        }

        public static void AddKeyToCategory(string category, string key)
        {
            var keys = GetKeysByCategory(category);
            if (!keys.Contains(key))
            {
                keys.Add(key);
                MemcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, $"{category}_keys", keys);
            }
        }

        public static void RemoveKeyFromCategory(string category, string key)
        {
            var keys = GetKeysByCategory(category);
            if (keys.Contains(key))
            {
                keys.Remove(key);
                MemcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, $"{category}_keys", keys);
            }
        }

    }
}
