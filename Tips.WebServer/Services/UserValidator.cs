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
using Nancy.Authentication.Forms;
using Nancy;

namespace Tips.WebServer.Services
{
    public class UserValidator : IUserValidator, IUserMapper
    {
        private IEventAggregator eventAgg;
        private Dictionary<string, Guid> formAuthCache;

        public UserValidator(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.formAuthCache = new Dictionary<string, Guid>();
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            var authed =
                from u in this.eventAgg.GetEvent<GetUserEvent>().Get(_ => true)
                let guid = ToGuid(u)
                where guid == identifier
                select ToBasicModel(u);
            return authed.FirstOrDefault();
        }

        public Guid ToGuid(IUser user)
        {
            if (this.formAuthCache.ContainsKey(user.Id) == false)
            {
                this.formAuthCache[user.Id] = Guid.NewGuid();
            }
            return this.formAuthCache[user.Id];
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
