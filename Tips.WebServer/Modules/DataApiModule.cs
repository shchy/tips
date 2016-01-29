using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;

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
                var model = this.Bind();

                

                using (var r = new System.IO.StreamReader(Request.Body))
                {
                    var a = r.ReadToEnd();
                    

                }
                return HttpStatusCode.OK;
            };


        }
    }
}
