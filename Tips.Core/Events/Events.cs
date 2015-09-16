using Haskellable.Code.Monads.Maybe;
using Microsoft.Practices.Prism.PubSubEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Core.Events
{
    public class GetUserEvent : PubSubEvent<Tuple<Action<IEnumerable<IUser>>,Func<IUser,bool>>>
    {
    }

    public class AddUserEvent : PubSubEvent<IUser>
    {
    }



    public static class PubSubEventExtention
    {
        public static IMaybe<TReturn> Publish<TReturn>(this PubSubEvent<Action<TReturn>> @this)
        {
            var v = default(TReturn);
            var isCallback = false;
            var callback = Act.New((TReturn x) => { v = x; isCallback = true; });
            @this.Publish(callback);
            return v.ToMaybe().Where(_ => isCallback);
        }

        public static IMaybe<TReturn> Publish<TReturn, T1>(
            this PubSubEvent<Tuple<Action<TReturn>, T1>> @this
            , T1 t1)
        {
            var v = default(TReturn);
            var isCallback = false;
            var callback = Act.New((TReturn x) => { v = x; isCallback = true; });
            @this.Publish(callback, t1);
            return v.ToMaybe().Where(_ => isCallback);
        }

        public static void Publish<T1,T2>(
            this PubSubEvent<Tuple<T1,T2>> @this
            , T1 t1, T2 t2)
        {
            @this.Publish(Tuple.Create(t1,t2));
        }

        public static void Publish<T1, T2, T3>(
            this PubSubEvent<Tuple<T1, T2, T3>> @this
            , T1 t1, T2 t2, T3 t3)
        {
            @this.Publish(Tuple.Create(t1, t2, t3));
        }
    }
}
