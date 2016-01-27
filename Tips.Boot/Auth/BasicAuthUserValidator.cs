using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Security;
using Tips.Model.Context;
using Tips.Core.Events;
using Prism.Events;

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
            var users = this.ea.GetEvent<GetUserEvent>().Get(_ => true);
            var user =
                from u in users
                where u.Id == username
                where u.Password == password
                select new UserIdentity
                {
                    UserName = username,
                    Claims = new[] { u.Role.ToString() }
                };
            return user.FirstOrDefault();
        }
    }

    class UserIdentity : IUserIdentity
    {
        public IEnumerable<string> Claims { get; set; }

        public string UserName { get; set; }
    }
}
