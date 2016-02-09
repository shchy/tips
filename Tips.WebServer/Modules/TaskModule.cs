using Nancy;
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
    public class TaskModule : NancyModule
    {
        public TaskModule(IEventAggregator eventAgg)
            :base("/task/")
        {
            this.RequiresAuthentication();

            Get["/{id}"] = prms =>
            {
                var id = prms.id;

                var task =
                    eventAgg.GetEvent<GetTaskWithRecordEvent>().Get(x => x.Id == id).FirstOrDefault();

                var user =
                   eventAgg.GetEvent<GetUserEvent>().Get(u => u.Id == Context.CurrentUser.UserName).FirstOrDefault();

                return View["Views/ProjectInTask"
                    , new
                    {
                        Auth = user,
                        Task = task,
                        Progress = (task.Records.Sum(x=>x.Value) / task.Value) * 100.0
                    }];
            };

        }
    }
}
