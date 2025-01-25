


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RocksDbSharp;
using System.Text;

namespace RocksDb_app.Models
{
    public class AppDbContext
    {
        public static RocksDb _db;
        private static string _databaseName ="databaseName";
        static AppDbContext()
        {
            _db = RocksDb.Open(new DbOptions()
                .SetCreateIfMissing(true), _databaseName);
        }
    }
}
