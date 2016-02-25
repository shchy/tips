using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

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

    public class GetOrder<TIN, TOUT>
    {
        public TIN Param { get; set; }
        public Action<TOUT> Callback { get; set; }
    }

    public class AuthOrder 
    {
        public IUser AuthUser { get; set; }
        public Action<IUser> Callback { get; set; }
    }

    public class AddOrder<T,With>
    {
        public T Model { get; set; }
        public With WithIn { get; set; }
    }

    public class DeleteOrder<T, With>
    {
        public T Model { get; set; }
        public With WithIn { get; set; }
    }
}
