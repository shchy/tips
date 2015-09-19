using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Nancy.Authentication.Forms;
using Tips.Model.Models;

namespace Tips.Web.Modules
{
    public class LoginModule : NancyModule
    {
        private IEventAggregator ea;
        private IUserToGuid userToGuid;

        public LoginModule()
        {
            this.ea = ServiceLocator.Current.GetInstance<IEventAggregator>();
            this.userToGuid = ServiceLocator.Current.GetInstance<IUserToGuid>();

            Get["/"] = prms =>
            {
                return View["Views/Login"];
            };

            Get["/login"] = prms =>
            {
                return Response.AsRedirect("/");
            };

            Get["/logout"] = prms =>
            {
                return this.LogoutAndRedirect("/");
            };

            Get["/login/{id}/pass/{pass}"] = prms =>
            {
                var id = (string)prms.id;
                var pass = (string)prms.pass;

                var user =
                    from ux in this.ea.GetEvent<GetUserEvent>().Publish(_ => true)
                    let a =
                        from u in ux
                        where u.Id == id
                        where u.Password == pass
                        select u
                    from u in a.FirstOrNothing()
                    select u;

                if (user.IsNothing)
                {
                    return Response.AsRedirect("/");
                }

                return
                    this.LoginAndRedirect(userToGuid.ToGuid(user.Return()), DateTime.Now.AddDays(7), "/manage/users");
            };
        }
    }

    public interface IUserToGuid
    {
        Guid ToGuid(IUser model);
    }
}
