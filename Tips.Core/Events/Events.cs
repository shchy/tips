using Haskellable.Code.Monads.Maybe;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Model.Models;

namespace Tips.Core.Events
{
    public class AuthUserEvent : PubSubEvent<AuthOrder>
    {
    }

    public class GetUserEvent : PubSubEvent<GetOrder<IUser>>
    {
    }

    public class AddUserEvent : PubSubEvent<IUser>
    {
    }


    public class AddProjectEvent : PubSubEvent<AddProjectOrder>
    {
    }

    public class UpdateProjectEvent : PubSubEvent<IProject>
    {
    }

    public class GetProjectEvent : PubSubEvent<GetOrder<IProject>>
    {
    }



    public static class PubSubEventExtention
    {
        public static IMaybe<TReturn> Get<TReturn>(this PubSubEvent<Action<TReturn>> @this)
        {
            var v = default(TReturn);
            var isCallback = false;
            var callback = Act.New((TReturn x) => { v = x; isCallback = true; });
            @this.Publish(callback);
            return v.ToMaybe().Where(_ => isCallback);
        }

        public static IUser Get(this PubSubEvent<AuthOrder> @this, IUser authUser)
        {
            var v = null as IUser;
            var callback = Act.New((IUser x) => { v = x; });
            var order = new AuthOrder
            {
                AuthUser = authUser,
                Callback = callback,
            };
            @this.Publish(order);
            return v;
        }

        public static IEnumerable<TReturn> Get<TReturn>(this PubSubEvent<GetOrder<TReturn>> @this, Func<TReturn,bool> predicate)
        {
            var v = Enumerable.Empty<TReturn>();
            var isCallback = false;
            var callback = Act.New((IEnumerable < TReturn> x) => { v = x; isCallback = true; });
            var order = new GetOrder<TReturn>
            {
                Callback = callback,
                Predicate = predicate
            };
            @this.Publish(order);

            return
                isCallback 
                ? v
                : Enumerable.Empty<TReturn>();
        }

        
    }
}
