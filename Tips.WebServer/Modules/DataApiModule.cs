using Nancy;
using Nancy.IO;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.WebServer.Modules
{
    public class DataApiModule : NancyModule
    {
        public DataApiModule(IEventAggregator eventAgg) : base("/api/")
        {
            this.RequiresAuthentication();
            

            Get["/users/"] = _ =>
            {
                var users =
                    from u in eventAgg.GetEvent<GetUserEvent>().Get(u => true)
                    select new { Id = u.Id, Name = u.Name, Role = u.Role.ToString() };
                return 
                    Response.AsJson(users.ToArray());
            };

            Post["/users/"] = _ =>
            {
                var model = this.Bind<User>();
                eventAgg.GetEvent<AddUserEvent>().Publish(model);

                return HttpStatusCode.OK;
            };

            Get["/projects/"] = _ =>
            {
                return
                    Response.AsJson(eventAgg.GetEvent<GetProjectEvent>().Get(p => true).ToArray());
            };

            Post["/projects/"] = _ =>
            {
                var project = 
                    Project.FromJson(this.Request.Body.ToStreamString())
                    .ToMaybe();

                project.On(eventAgg.GetEvent<UpdateProjectEvent>().Publish);

                return project.IsSomething ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;

            };

        }
    }

    public class LoginApiModule : NancyModule
    {
        public LoginApiModule(IEventAggregator eventAgg) : base("/api/")
        {
            Post["/login/"] = _ =>
            {
                var model = this.Bind<User>();

                var user =
                    eventAgg.GetEvent<GetUserEvent>()
                    .Get(u => u.Id == model.Id && u.Password == model.Password)
                    .FirstOrDefault();

                return
                    Response.AsJson(user, HttpStatusCode.OK);
            };
        }

    }

    public static class NancyModuleExtension
    {
        public static string ToStreamString(this RequestStream @this)
        {
            using (var r = new StreamReader(@this))
            {
                return r.ReadToEnd();
            }
        }
    }
}
