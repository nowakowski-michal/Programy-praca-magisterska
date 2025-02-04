

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Neo4j_app.Models
{
    public class AppDbContext
    {
        private static string uri = "neo4j://localhost:7687";
        private static  string username = "neo4j"; 
        private static string password = "password";
        public static IDriver _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));

    }
}
