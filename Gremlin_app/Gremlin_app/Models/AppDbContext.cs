

using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.Json;


namespace Gremlin_app.Models
{
    public class JanusGraphRelationIdentifierDeserializer : IGraphSONDeserializer
    {
        public dynamic Objectify(JToken graphsonObject, GraphSONReader reader)
        {
            return graphsonObject.ToString();
        }

        public dynamic Objectify(JsonElement graphsonObject, GraphSONReader reader)
        {
            return graphsonObject.ToString();
        }
    }
    public class AppDbContext
    {
        private static GremlinServer gremlinServer = new GremlinServer("localhost", 8182);
        // Definiowanie niestandardowego deserializatora
        private static Dictionary<string, IGraphSONDeserializer> customDeserializers = new Dictionary<string, IGraphSONDeserializer>
        {
            { "janusgraph:RelationIdentifier", new JanusGraphRelationIdentifierDeserializer() }
        };

        // Tworzenie instancji GraphSON3Reader z niestandardowym deserializatorem
        private static GraphSON3Reader graphSONReader = new GraphSON3Reader(customDeserializers);
        private static GraphSON3Writer graphSONWriter = new GraphSON3Writer();
        public static GremlinClient  client = new GremlinClient(gremlinServer, graphSONReader, graphSONWriter);
    }
}
