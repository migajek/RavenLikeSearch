using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raven.Abstractions.Util;
using Raven.Client;
using Raven.Client.Embedded;

namespace RavenPlayground
{
    public class Entity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public override bool Equals(object obj)
        {
            return (obj as Entity)?.Id == Id;
        }
                
        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }

    public static class EntExt
    {
        public static string Dump(this IQueryable<Entity> q)
        {
            return q.AsEnumerable().Dump();
        }

        public static string Dump(this IEnumerable<Entity> q)
        {
            return String.Join(", ", q.Select(x => x.Name));
        }
    }

    class Program
    {
        private static readonly IEnumerable<Entity> _entities = new[]
        {
            new Entity() {Id = "1", Name = "abc"},
            new Entity() {Id = "2", Name = "def"},
            new Entity() {Id = "3", Name = "abc def"},
            new Entity() {Id = "4", Name = "def abc"},
            new Entity() {Id = "5", Name = "zz def zz"},
            new Entity() {Id = "6", Name = "zz abc def zz"}
        };

        private static string Compare(IQueryable<Entity> actual, IEnumerable<Entity> expected)
        {
            var actualEvaluated = actual.OrderBy(x => x.Id).ToArray().AsQueryable();
            var expectedEvaluated = expected.OrderBy(x => x.Id).ToArray().AsQueryable();
            if (actualEvaluated.SequenceEqual(expectedEvaluated))
                return "COMPARE OK";

            var extras = actualEvaluated.Where(x => !expectedEvaluated.Contains(x)).ToArray();
            var missing = expectedEvaluated.Where(x => !actualEvaluated.Contains(x)).ToArray();
            return
                $"ACTUAL: {actualEvaluated.Dump()}\nEXPECTED: {expectedEvaluated.Dump()}\nDIFF: {String.Join(", ", extras.Select(x => "+" + x.Name))}, {String.Join(", ", missing.Select(x => "-" + x.Name))}";
        }

        static void Main(string[] args)
        {            
            const string dir = @"C:\tmp\rave";
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            var store = new EmbeddableDocumentStore
            {
                DataDirectory = dir,
                RunInMemory = true,
            };
            store.Conventions.DefaultQueryingConsistency = Raven.Client.Document.ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite;
            store.Configuration.Storage.Voron.AllowOn32Bits = true;
            store.Initialize();

            using (var sess = store.OpenSession())
            {
                foreach (var entity in _entities)
                {
                    sess.Store(entity);
                }                
                sess.SaveChanges();
            }

            using (var sess = store.OpenSession())
            {
                Console.WriteLine("*abc*");
                var e1 = Compare(sess.Query<Entity>()
                    .Search(x => x.Name, "*abc*", escapeQueryOptions: EscapeQueryOptions.AllowAllWildcards),
                    _entities.Where(x => x.Name.Contains("abc")));
                Console.WriteLine(e1);

                Console.WriteLine("*abc def* #1 - AllowAllWildcards");
                var e2 = Compare(sess.Query<Entity>()
                    .Search(x => x.Name, "*abc def*", escapeQueryOptions: EscapeQueryOptions.AllowAllWildcards),
                    _entities.Where(x => x.Name.Contains("abc def")));
                Console.WriteLine(e2);

                Console.WriteLine("*abc def* #2 - Escape + RawQuery");
                var escaped = RavenQuery.Escape("abc def");
                var e3 = Compare(sess.Query<Entity>()
                    .Search(x => x.Name, $"*{escaped}*", escapeQueryOptions: EscapeQueryOptions.RawQuery),
                    _entities.Where(x => x.Name.Contains("abc def")));
                Console.WriteLine(e3);

                Console.WriteLine("*abc def* #3 - Escape + Replace space + RawQuery");
                escaped = RavenQuery.Escape("abc def").Replace(' ', '\\');
                var e4 = Compare(sess.Query<Entity>()
                    .Search(x => x.Name, $"*{escaped}*", escapeQueryOptions: EscapeQueryOptions.RawQuery),
                    _entities.Where(x => x.Name.Contains("abc def")));
                Console.WriteLine(e4);
            }

            Console.ReadLine();
        }
    }
}
