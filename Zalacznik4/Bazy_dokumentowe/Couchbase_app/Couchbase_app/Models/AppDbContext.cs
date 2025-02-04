using Couchbase;
using Couchbase.Management.Buckets;
using Couchbase.Management.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Couchbase_app.Models
{
    public class AppDbContext
    {
        //nazwa koszyka oraz przestrzeni na któej umieszczone są dokumenty(kolekcje z danymi)
        private const string BucketName = "DronesBucket";
        public static string ScopeName = "DronesScope";
        //kolekcje danych
        private static readonly string[] Collections = { "Drones", "Insurances", "Locations", "Missions", "Pilots", "PilotMissions" };

        public static Cluster _cluster;
        public static IBucket _bucket;

        public AppDbContext(string username, string password)
        {
            var options = new ClusterOptions
            {
                UserName = username,
                Password = password
            };
            //polączenie z serwerem pracującym na localhost
            _cluster = (Cluster?)Cluster.ConnectAsync("couchbase://localhost", options).Result;
        }
        //funkcja tworząca koszyk na dane 
        public async Task InitializeAsync()
        {
            var bucketManager = _cluster.Buckets;

            if (!await BucketExistsAsync(bucketManager, BucketName))
            {
                await bucketManager.CreateBucketAsync(new BucketSettings
                {
                    Name = BucketName,
                    BucketType = BucketType.Couchbase,
                    RamQuotaMB = 1000
                });
            }

            _bucket = await _cluster.BucketAsync(BucketName);
            await EnsureScopeAndCollectionsAsync(_bucket);
        }
        //funkcja tworząca kolekcje w danym koszyku
        public static async Task EnsureScopeAndCollectionsAsync(IBucket bucket)
        {
            var collectionManager = bucket.Collections;

            try
            {
                await collectionManager.CreateScopeAsync(ScopeName);
            }
            catch (ScopeExistsException) { }

            foreach (var collection in Collections)
            {
                try
                {
                    await collectionManager.CreateCollectionAsync(new CollectionSpec(ScopeName, collection));
                }
                catch (CollectionExistsException) { }
            }
        }
        //pobranie aktualnego bucketa z danymi
        private static async Task<bool> BucketExistsAsync(IBucketManager bucketManager, string bucketName)
        {
            var buckets = await bucketManager.GetAllBucketsAsync();
            return buckets.ContainsKey(bucketName);
        }

        public async Task CloseAsync()
        {
            await _cluster.DisposeAsync();
        }
    }
}