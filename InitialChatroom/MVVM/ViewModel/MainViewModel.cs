﻿  using ChatClient.MVVM.Core;
using ChatClient.MVVM.Model;
using ChatClient.Net;
using ChatClient.Net.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace ChatClient.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public ObservableCollection<UserModel> Users { get; set; }
        public ObservableCollection<string> Messages { get; set; }
        public RelayCommand ConnectToServerCommand {  get; set; }
        public RelayCommand SendMessageCommand { get; set; }

        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ShutdownWindowCommand { get; set; }

        public RelayCommand MaximizeWindowCommand { get; set; }

        public RelayCommand MinimizeWindowCommand { get; set; }

        public string Username { get; set; }

        private string _message;

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }


        private Notification _notification;


        private Server _server;
        public MainViewModel()
        {




            Users = new ObservableCollection<UserModel>();
            Messages = new ObservableCollection<string>();
            _server = new Server();
            _notification = new Notification();

            Application.Current.MainWindow.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            MoveWindowCommand = new RelayCommand(o =>
            {
                if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                {
                    
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                }
                Application.Current.MainWindow.DragMove();
            });

            ShutdownWindowCommand = new RelayCommand(o =>
            {
                _server._client.Close();
                Application.Current.Shutdown();
            });

            MaximizeWindowCommand = new RelayCommand(o =>
            {
                if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                }
            });

            MinimizeWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.WindowState = WindowState.Minimized; });


            _server.connectedEvent += UserConnected;
            _server.msgReceivedEvent += MessageReceived;
            _server.userDisconnectEvent += UserDisconnected;
            _server.invalidUsernameEvent += InvalidUsername;
            _server.usernameAlreadyTakenEvent += UsernameAlreadyTaken;
            ConnectToServerCommand = new RelayCommand(o => _server.ConnectToServer(Username), o => !string.IsNullOrEmpty(Username));
            SendMessageCommand = new RelayCommand(o =>
                {
                    _server.SendMessageToServer(Message);
                    Message = "";
                },
                o => !string.IsNullOrEmpty(Message));

        }

        private void UserDisconnected()
        {
            var uid = _server.PacketReader.ReadMessage();
            var username = Users.Where(x =>x.UID == uid).FirstOrDefault();
            if (username == null) { return; }
            Application.Current.Dispatcher.Invoke(() => Users.Remove(username));
        }

        private void MessageReceived()
        {
            var msg = _server.PacketReader.ReadMessage();
            Application.Current.Dispatcher.Invoke(() => Messages.Add(msg));
        }

        private void UserConnected()
        {
            var user = new UserModel
            {
                Username = _server.PacketReader.ReadMessage(),
                UID = _server.PacketReader.ReadMessage(),
            };

            if (!Users.Any(x => x.UID == user.UID))
            {
                Application.Current.Dispatcher.Invoke(() => Users.Add(user));
            }

        }

        public Notification Notification
        {
            get { return _notification; }
            set { _notification = value; }
        }

        private void InvalidUsername()
        {
            Task.Run(() =>
            {
                this.Notification.NotificationMsg = "Invalid Username";
                Thread.Sleep(8000);
                this.Notification.NotificationMsg = "";
            });
        }

        private void UsernameAlreadyTaken()
        {

            string ErrorMessage = _server.PacketReader.ReadMessage();
            Task.Run(() =>
            {
                this.Notification.NotificationMsg = $"{ErrorMessage}";
                Thread.Sleep(8000);
                this.Notification.NotificationMsg = "";
            });
            _server = new Server();
            _server.connectedEvent += UserConnected;
            _server.msgReceivedEvent += MessageReceived;
            _server.userDisconnectEvent += UserDisconnected;
            _server.invalidUsernameEvent += InvalidUsername;
            _server.usernameAlreadyTakenEvent += UsernameAlreadyTaken;
            ConnectToServerCommand = new RelayCommand(o => _server.ConnectToServer(Username), o => !string.IsNullOrEmpty(Username));
            SendMessageCommand = new RelayCommand(o => _server.SendMessageToServer(Message), o => !string.IsNullOrEmpty(Message));
        }
    }
}
