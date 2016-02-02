using Nancy.Authentication.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Security;
using Prism.Events;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.WebServer.Services
{
    public class UserValidator : IUserValidator
    {
        private IEventAggregator eventAgg;

        public UserValidator(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
        }
        public IUserIdentity Validate(string username, string password)
        {
            return
                this.eventAgg.GetEvent<GetUserEvent>()
                    .Get(u => u.Id == username && u.Password == password)
                    .Select(ToBasicModel)
                    .FirstOrDefault();
        }

        private IUserIdentity ToBasicModel(IUser user)
        {
            return
                new UserIdentity
                {
                    UserName = user.Id,
                    Claims = new[] { user.Role.ToString() },
                };
        }
    }

    class UserIdentity : IUserIdentity
    {
        public IEnumerable<string> Claims { get; set; }

        public string UserName { get; set; }
    }


}
