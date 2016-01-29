using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace tips.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var a = "http://localhost:9876/";
            var c = new Tips.Model.Context.WebApiContext(a, new Tips.Model.Models.User { Id = "admin", Password = "admin"  });
            var test = c.GetUser(_ => true);
            c.AddUser(new Tips.Model.Models.User
            {
                Id = "test",
                Name = "testest",
                Password = "test",
                Role = Tips.Model.Models.UserRole.Normal,
            });


            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }
}
