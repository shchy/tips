using Nancy.Authentication.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Security;
using Tips.Model.Context;
using Microsoft.Practices.ServiceLocation;
using Tips.Web.Modules;

namespace Tips.Boot.Auth
{
    class FormAuthUserMapper : IUserMapper
    {
        private IDataBaseContext context;
        private IUserToGuid userToGuid;

        public FormAuthUserMapper(IDataBaseContext context, IUserToGuid userToGuid)
        {
            this.context = context;
            this.userToGuid = userToGuid;
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            var users =
                from u in this.context.GetUser()
                where this.userToGuid.ToGuid(u) == identifier
                select new UserIdentity { UserName = u.Id, Claims = new[] { u.Role.ToString() } };

            return users.FirstOrDefault();
        }
    }
}
