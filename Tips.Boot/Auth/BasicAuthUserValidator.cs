using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Security;
using Tips.Model.Context;
using Microsoft.Practices.Prism.PubSubEvents;
using Tips.Core.Events;

namespace Tips.Boot.Auth
{
    class BasicAuthUserValidator : IUserValidator
    {
        private IDataBaseContext context;
        private IEventAggregator ea;

        public BasicAuthUserValidator(IDataBaseContext context, IEventAggregator ea)
        {
            this.ea = ea;
            this.context = context;
        }

        public IUserIdentity Validate(string username, string password)
        {
            var maybeUsers = this.ea.GetEvent<GetUserEvent>().Publish(_ => true);
            var user =
                from users in maybeUsers
                let validUsers =
                    from u in users
                    where u.Id == username
                    where u.Password == password
                    select new UserIdentity
                    {
                        UserName = username,
                        Claims = new[] { u.Role.ToString() }
                    }
                from validUser in validUsers.FirstOrNothing()
                select validUser;
            return user.Return(() => null);
        }
    }

    class UserIdentity : IUserIdentity
    {
        public IEnumerable<string> Claims { get; set; }

        public string UserName { get; set; }
    }
}
