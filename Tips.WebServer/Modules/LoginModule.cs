using Nancy;
using Nancy.Authentication.Forms;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.WebServer.Services;

namespace Tips.WebServer.Modules
{
    public class LoginModule : NancyModule
    {
        public LoginModule(IEventAggregator eventAgg, IUserMapper mapper)
        {
            Get["/"] = prms =>
            {
                return View["Views/Login"];
            };


            Get["/logout"] = prms =>
            {
                return this.LogoutAndRedirect("/");
            };

            Get["/login/{id}/pass/{pass}"] = prms =>
            {
                var id = (string)prms.id;
                var pass = (string)prms.pass;

                var findId =
                    (from u in eventAgg.GetEvent<GetUserEvent>().Get(_ => true)
                     where u.Id == id
                     where u.Password == pass
                     select (mapper as UserValidator).ToGuid(u)).FirstOrNothing();

                if (findId.IsNothing)
                {
                    return Response.AsRedirect("/");
                }

                return
                    this.LoginAndRedirect(findId.Return(), DateTime.Now.AddDays(7), "/home/");
            };
        }
    }
}
