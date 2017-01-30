using System;
using System.IO;
using System.Linq;
using Raven.Abstractions.Extensions;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Linq;
using Raven.Imports.Newtonsoft.Json;

namespace RavenPlayground
{
    public class Entity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ArticleTemplateId { get; set; }        
    }

    
    class Program
    {
        private const int MaxDocuments = 1000;
        private const string DumpFileName = "dump.json";
        private static readonly string[] Help = {
            "q to quit",
            $"i to import data from dumpfile ({DumpFileName})",
            "c to create broken index",
            "r to remove broken index",
            $"e to export data to dumpfile ({DumpFileName})",            
        };

        static void Main(string[] args)
        {
            new[]
            {
                "When running for the first time, please do the following:",
                "1. run import (i)",
                "2. ensure you've got 5 items found in both cases (if not, press enter to re-run query - the eventual consistency...)",
                "3. create broken index (c)",
                "4. the query ravendb-side should should return no results (invalid behavior!)",
                "5. remove broken index (r)",
                "6. the query should behave properly again",
                "---------",""
            }.ForEach(Console.WriteLine);

            var store = InitializeStore();

            string cmd = "";
            while (cmd != "q")
            {
                Help.ForEach(Console.WriteLine);
                Console.WriteLine("");
                switch (cmd)
                {
                    case "c":
                        CreateInvalidIndex(store);
                        break;

                    case "r":
                        RemoveInvalidIndex(store);
                        break;

                    case "e":
                        ExportDataToFile(store);
                        break;

                    case "l":
                        ImportDataFromFile(store);
                        break;
                }

                RunQuery(store);

                Console.WriteLine("");
                cmd = Console.ReadLine();
                Console.WriteLine("");
            }
        }

        private static void ImportDataFromFile(IDocumentStore store)
        {
            if (!File.Exists(DumpFileName))
            {
                Console.WriteLine($"Dump file {DumpFileName} does not exist");
                return;
            }
            var entities = JsonConvert.DeserializeObject<Entity[]>(File.ReadAllText(DumpFileName));
            Console.WriteLine($"Bulk insert {entities.Length} entitites");
            using (var sess = store.BulkInsert())
            {
                entities.ForEach(x => sess.Store(x));
            }
            Console.WriteLine($"Bulk insert done");
        }

        private static IDocumentStore InitializeStore()
        {
            const string databaseName = "demo-db2";
            var store = new DocumentStore
            {
                Url = "http://localhost:8080/",
                DefaultDatabase = databaseName,
            };
            store.Initialize();
            store.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(databaseName);
            return store;
        }

        private static void RunQuery(IDocumentStore store)
        {
            Console.WriteLine("\nRunning query: ");
            using (var sess = store.OpenSession())
            {
                var whereServerSide = sess.Query<Entity>()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .Take(MaxDocuments)
                    .ToList();


                Console.WriteLine($"Got {whereServerSide.Count} items - ravendb");

                var whereInMemory = sess.Query<Entity>()
                    .Take(MaxDocuments)
                    .ToList()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .ToList();
                Console.WriteLine($"Got {whereInMemory.Count} items - in memory");
            }
        }        

        private static void ExportDataToFile(IDocumentStore store)
        {
            Console.WriteLine("Dumping data");
            using (var sess = store.OpenSession())
            {
                var entities = sess.Query<Entity>().Take(MaxDocuments).ToList();
                if (!entities.Any())
                {
                    Console.WriteLine("No entities, skipping.");
                    return;
                }
                File.WriteAllText(DumpFileName, JsonConvert.SerializeObject(entities));
                Console.WriteLine($"Data dumped to {DumpFileName}");
            }
        }

        private static void RemoveInvalidIndex(IDocumentStore store)
        {
            Console.WriteLine("Listing indexes");
            var indexes =
                store.DatabaseCommands.GetIndexNames(0, 1024).Where(x => x.ToLower().Contains("foo")).ToArray();
            Console.WriteLine($"Found {indexes.Length} matching, removing them");
            indexes.ForEach(x => Console.WriteLine($"\t{x}"));
            indexes.ForEach(x => store.DatabaseCommands.DeleteIndex(x));
            Console.WriteLine("Removing done");
        }

        private static void CreateInvalidIndex(IDocumentStore store)
        {
            Console.WriteLine("Querying for FooField over Entity");
            using (var sess = store.OpenSession())
            {
                sess.Advanced
                    .DocumentQuery<Entity>()
                    .WhereEquals("FooField", "5")
                    .ToList();
                sess.SaveChanges();
            }
            Console.WriteLine("Querying done");
        }
    }
}
