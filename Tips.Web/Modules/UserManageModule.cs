using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.Web.Modules
{
    public class UserManageModule : NancyModule
    {
        private IEventAggregator ea;

        public UserManageModule()
            :base("/manage/users/")
        {
            this.ea = ServiceLocator.Current.GetInstance<IEventAggregator>();

            // 認証を必要とする
            this.RequiresAuthentication();

            Get["/"] = args =>
            {
                var users =
                    this.ea.GetEvent<GetUserEvent>()
                        .Publish(Fn.New((IUser _) => true));
                var model = new
                {
                    Users = users.Return(Enumerable.Empty<IUser>())
                };
                return View["Views/UserList", model];
            };

            Get["/add/"] = args =>
            {
                var users =
                    this.ea.GetEvent<GetUserEvent>()
                        .Publish(Fn.New((IUser _) => true));

                return View["Views/UserAdd"];
            };

            Get["/add/id/{id}/pass/{password}/name/{name}/isadmin/{isAdmin}"] = prms =>
            {
                var id = (string)prms.id;
                var password = (string)prms.password;
                var name = (string)prms.name;
                var isAdmin = (bool)prms.isAdmin;

                this.ea.GetEvent<AddUserEvent>()
                        .Publish(new User
                        {
                            Id = id,
                            Password = password,
                            Name = name,
                            Role = isAdmin ? UserRole.Admin : UserRole.Normal
                        });

                return Response.AsRedirect("/manage/users/");
            };
        }
    }
}
