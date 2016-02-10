using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using Tips.Desktop.Modules;
using Tips.Core.Events;
using Tips.Model.Models;

namespace Tips.Desktop.ViewModels
{
    public class LoginViewModel : BindableBase
    {
        private IEventAggregator eventAgg;

        public DelegateCommand LoginCommand { get; private set; }
        public string Id { get; set; }
        public string Password { get; set; }
        public LoginViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.LoginCommand =
                new DelegateCommand(Login);
        }

        private void Login()
        {
            var tempUser = new User
            {
                Id = this.Id,
                Password = this.Password
            };
            var authUser =
                this.eventAgg.GetEvent<AuthUserEvent>().Get(tempUser);

            if (authUser == null)
            {
                return;
            }
            this.eventAgg.GetEvent<SetAuthUserEvent>().Publish(authUser);
            this.eventAgg.GetEvent<NavigateEvent>().Publish(ViewNames.PROJECTS);
        }
    }
}
