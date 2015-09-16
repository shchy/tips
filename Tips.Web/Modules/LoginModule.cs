using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Web.Modules
{
    public class LoginModule : NancyModule
    {
        private IEventAggregator ea;

        public LoginModule()
        {
            this.ea = ServiceLocator.Current.GetInstance<IEventAggregator>();

            Get["/"] = prms =>
            {
                return null;
            };
        }
    }
}
