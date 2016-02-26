using Microsoft.Practices.Unity;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tips.Core.Events;
using Tips.Core.Services;

namespace Tips.Core.Controllers
{
    public class WorkdayServiceController
    {
        private IEventAggregator eventAgg;
        private string workdaySettingFolder;

        public WorkdayServiceController(
            IEventAggregator eventAgg
            , [Dependency("workdaySettingFolder")] string workdaySettingFolder)
        {
            this.eventAgg = eventAgg;
            this.workdaySettingFolder = workdaySettingFolder;

            eventAgg.GetEvent<GetWorkdayContextEvent>().Subscribe(GetWorkdayContext, true);
        }

        private void GetWorkdayContext(GetOrder<int, IWorkdayContext> order)
        {
            var callback = order.Callback;
            var projectId = order.Param;
            var context = new WorkdayContext(Path.Combine(this.workdaySettingFolder, projectId + ".json"));
            callback(context);
        }
    }
}
