using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.MVVM.Model
{
    public class Notification : INotifyPropertyChanged
    {
        private string _notification;

        public string NotificationMsg
        {
            get { return _notification; }
            set
            {
                _notification = value;
                this.OnPropertyChanged("NotificationMsg");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

}
