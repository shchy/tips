using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tips.WebServer
{
    class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var port = 9876;
            if (args.Length > 0)
            {
                int.TryParse(args[0], out port);
            }

            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            var uri = string.Format("http://+:{0}", port);
            using (WebApp.Start<Startup>(uri))
            {
                Console.WriteLine("Running port: {0}", port);
                Console.WriteLine("Started");
#if DEBUG
                var a = System.Diagnostics.Process.GetProcessesByName("tips.Desktop");
                a.ForEach(x => x.CloseMainWindow());
                System.Diagnostics.Process.Start("../../../tips.Desktop/bin/Debug\\tips.Desktop.exe");
#endif
                _quitEvent.WaitOne();

            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var option = new Nancy.Owin.NancyOptions();
            option.Bootstrapper = new Bootstrapper();
            app.UseNancy(option);
        }
    }
}
