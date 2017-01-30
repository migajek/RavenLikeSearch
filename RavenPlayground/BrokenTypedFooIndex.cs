using System.Linq;
using Raven.Client.Indexes;

namespace RavenPlayground
{
    public class BrokenTypedFooIndex : AbstractIndexCreationTask<Entity>
    {
        public BrokenTypedFooIndex()
        {
            Map = docs => 
                from doc in docs
                select new
                {
                    FooField2 = doc.Name,                                    
                };
        }        
    }
}