using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Raven.Abstractions.Data;
using Raven.Abstractions.Util;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Linq;

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
        
        static void Main(string[] args)
        {                        
            var sourceStore = new DocumentStore
            {
                Url = "http://localhost:8080/",
                DefaultDatabase = "Tenant-10",
                Conventions =
                {
                    FindTypeTagName = type =>
                    {
                        if (type == typeof (Entity))
                            return
                                DocumentConvention.DefaultTransformTypeTagNameToDocumentKeyPrefix(
                                    "Abax_Worker_Services_Documents_Projections_Article_Model_Article");
                        return DocumentConvention.DefaultTypeTagName(type);
                    }
                },
            };
            sourceStore.Initialize();

            var outputStore = new DocumentStore
            {
                Url = "http://localhost:8080/",
                DefaultDatabase = "demo-db",               
            };
            outputStore.Initialize();
            outputStore.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists("demo-db");


            //using (var sess = sourceStore.OpenSession())
            //{
            //    var arts = sess.Query<Entity>().Take(1000).ToList();
            //    arts.ForEach(art =>
            //    {
            //        using (var sess2 = outputStore.OpenSession())
            //        {
            //            sess2.Store(art);                        
            //            sess2.SaveChanges();
            //        }
            //    });
            //}
            //Console.WriteLine("Pumping done");

            Console.WriteLine("SOURCE: ");
            using (var sess = sourceStore.OpenSession())
            {
                var whereServerSide = sess.Query<Entity>()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .Take(1000)
                    .ToList();                    


                Console.WriteLine($"Got {whereServerSide.Count} items - ravendb");

                var whereInMemory = sess.Query<Entity>()
                    .Take(1000)
                    .ToList()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .ToList();
                Console.WriteLine($"Got {whereInMemory.Count} items - in memory");
            }

            Console.WriteLine("\n\nDEST: !!!");
            using (var sess = outputStore.OpenSession())
            {
                var whereServerSide = sess.Query<Entity>()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .Take(1000)
                    .ToList();


                Console.WriteLine($"Got {whereServerSide.Count} items - ravendb");

                var whereInMemory = sess.Query<Entity>()
                    .Take(1000)
                    .ToList()
                    .Where(x => x.Id.In(420, 406, 262, 263, 264))
                    .ToList();
                Console.WriteLine($"Got {whereInMemory.Count} items - in memory");
            }


            //using (var sess = store.OpenSession())
            //{
            //    if (sess.Query<Entity>().Count() < 1000)
            //        Enumerable.Range(1, 1024)
            //            .Select(x => new Entity()
            //            {
            //                ArticleTemplateId = x < 850 ? x + 200 : (long?) null,
            //                Id = x+10,
            //                Name = $"Article #{x}"
            //            })
            //            .ToList()
            //            .ForEach(x => sess.Store(x));
            //    sess.SaveChanges();
            //}

            //using (var sess = store.OpenSession())
            //{
            //    sess.Advanced.MaxNumberOfRequestsPerSession = 1000;
            //    //var ar = sess.Query<Entity>().Where(x => x.Id.In(262, 263, 264)).ToArray();
            //    var chunks = Enumerable.Range(1, 1024)
            //        .Select(x => x + 10)
            //        .Select((x, i) => new {Index = i, Value = (long)x})
            //        .GroupBy(x => x.Index/3)
            //        .Select(x => x.Select(v => v.Value).ToList());

            //    foreach (var chunk in chunks)
            //    {
            //        var ar = sess.Query<Entity>().Where(x => x.Id.In(chunk)).ToArray();
            //        var missings = chunk.Where(idx => ar.All(ent => ent.Id != idx));
            //        foreach (var missing in missings)
            //        {
            //            Console.WriteLine($"Missing #{missing}");
            //        }                    
            //    }
            //    Console.WriteLine("Done");

            //}

            Console.ReadLine();
        }
    }
}
