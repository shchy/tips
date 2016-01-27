using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Core.Events
{
    public class AddProjectOrder
    {
        public string Name { get; set; }
        public string Describe { get; set; }
    }

    public class GetOrder<T>
    {
        public Action<IEnumerable<T>> Callback { get; set; }
        public Func<T,bool> Predicate { get; set; }
    }
}
