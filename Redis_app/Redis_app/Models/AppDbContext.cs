using StackExchange.Redis;

namespace Redis_app.Models
{
    public class AppDbContext
    {
        public static string redisConnectionString = "localhost,allowAdmin=true";
        public static ConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        public static IDatabase redisDatabase = redisConnection.GetDatabase();
        public static IServer server = redisConnection.GetServer("localhost", 6379);

    }
}
