using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Tips.Core.Events;
using Tips.Desktop.Modules;
using Tips.Model.Models;

namespace Tips.Desktop.ViewModels
{
    [PropertyChanged.ImplementPropertyChanged]
    public class UserViewModel : BindableBase, INavigationAware
    {
        private IEventAggregator eventAgg;
        public DelegateCommand SelectIconFileCommand { get; private set; }
        public DelegateCommand UpdateCommand { get; private set; }
        public IUser User { get; private set; }
        public string SelectedIconFile { get; private set; }

        public UserViewModel(IEventAggregator eventAgg)
        {
            this.eventAgg = eventAgg;
            this.SelectIconFileCommand = new DelegateCommand(SelectIconFile);
            this.UpdateCommand = new DelegateCommand(UpdateUser);
            
               
        }

        private void UpdateUser()
        {
            using (var fs = new FileStream(this.SelectedIconFile, FileMode.Open, FileAccess.Read))
            {
                var img = new Bitmap(fs);
                var converter = new ImageConverter();
                var bytes = converter.ConvertTo(img, typeof(byte[])) as byte[];
                var base64String = Convert.ToBase64String(bytes);
                this.eventAgg.GetEvent<AddUserIconEvent>().Publish(new AddUserWithIcon
                {
                    UserId = this.User.Id,
                    Base64BytesByImage = base64String,
                });


            }
        }

        private void SelectIconFile()
        {
            this.eventAgg.GetEvent<SelectFileEvent>().Publish(selectFile =>
            {
                if (string.IsNullOrWhiteSpace(selectFile))
                {
                    return;
                }
                this.SelectedIconFile = selectFile;
            });
        }


        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.User = null;
            var query = navigationContext.TryToGetUser(this.eventAgg);
            query.On(x => this.User = x);
        }
    }
}
