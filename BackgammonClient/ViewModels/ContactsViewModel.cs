﻿using BackgammonClient.BL;
using BackgammonClient.Converters;
using BackgammonClient.Helpers;
using BackgammonClient.Models;
using BackgammonClient.Utils;
using BackgammonClient.Views;
using General.Emuns;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BackgammonClient.ViewModels
{
    class ContactsViewModel : ViewModelPropertyChanged
    {
        private bool isChat;

        public ICommand LogoutCommand { get; set; }
        public ICommand OpenChatCommand { get; set; }
        public ICommand OpenGameCommand { get; set; }
        public UserForView ChosenContact { get; set; }

        private IFrameNavigationService _navigationService;
        ClientUserManager _userManager;
        ClientInteractionManager _interactionManager;
        private ObservableCollection<UserForView> _contactList;
        public ObservableCollection<UserForView> ContactList
        {
            get
            {
                return _contactList;
            }
            set
            {
                _contactList = value;
                OnPropertyChanged();
            }
        }

        public string UserTitle { get; set; }
        //ctor
        public ContactsViewModel(IFrameNavigationService navigationService)
        {
            _navigationService = navigationService;
            _userManager = new ClientUserManager();
            _interactionManager = new ClientInteractionManager();

            ContactList = ConvertUserForUserView.ConvertUser(_userManager.GetContactList());

            _userManager.RegisterNotifyEvent(ContactUptaded);
            _interactionManager.RegisterInvitationResponseEvent(HandleUserResponse);
            _interactionManager.RegisterChatRequestEvent(AgreeChatRequest);

            LogoutCommand = new RelayCommand(Logout);
            OpenChatCommand = new RelayCommand(OpenChat);
            OpenGameCommand = new RelayCommand(OpenGame);
            UserTitle = $"Welcome {ClientUserManager.CurrentUser}";
        }


        // update contacts list.
        private void ContactUptaded(Dictionary<string, UserState> dictionary)
        {
            ContactList = ConvertUserForUserView.ConvertUser(dictionary);
        }
        //Sand request to another user to chat with him.
        private void Open(bool isChat)
        {
            if (ChosenContact != null)
            {
                if (ChosenContact.State == UserState.offline)
                {
                    MessageBox.Show("User is offline.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (ChosenContact.State == UserState.busy)
                {
                    MessageBox.Show("User is not available.");
                }
                else
                {
                    ClientUserManager.UserToChatWith = ChosenContact.UserName;
                    _userManager.ChangeUserStatus(UserState.busy);
                    _interactionManager.SendRequest(isChat);
                }
            }
            else
            {
                MessageBox.Show("you need to choose user.");
            }
        }

        //For reciver, after he agree to chat request.
        private void AgreeChatRequest(bool isChat)
        {

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isChat) _navigationService.NavigateTo("Chat");
                else _navigationService.NavigateTo("Game");
            }));

        }

        //For sender, after reciver agree to chat request.
        private void HandleUserResponse(bool response)
        {
            if (response)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (isChat) _navigationService.NavigateTo("Chat");
                    else _navigationService.NavigateTo("Game");
                }));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("user refused to join your chat.");
                }));
            }
        }

        private void OpenChat()
        {
            isChat = true;
            Open(isChat);
        }

        private void OpenGame()
        {
            isChat = false;
            Open(false);
        }

        //Logout to register page.
        private void Logout()
        {
            if (_userManager.InvokeLogout())
            {
                _navigationService.GoBack();
            }
        }
    }
}
