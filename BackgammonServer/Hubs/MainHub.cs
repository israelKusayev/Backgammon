﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BackgammonServer.BL;
using BackgammonServer.Models;
using General.Emuns;
using General.Interfaces;
using General.Models;
using Microsoft.AspNet.SignalR;

namespace BackgammonServer.Hubs
{
    public class MainHub : Hub
    {
        ServerUserManager _userManager = ServerUserManager.Instance;
        ServerGameManager _gameManager = ServerGameManager.Instance;
        #region User

        // Register. 
        public bool Register(User newUser)
        {
            if (_userManager.RegisterToDb(newUser))
            {
                _userManager.AddConectionId(Context.ConnectionId, newUser.UserName);
                NotifyUserStateChanged();
                return true;
            }
            return false;
        }

        // Login.
        public string Login(User user)
        {
            string error = _userManager.Login(user);
            if (error == null)
            {
                _userManager.AddConectionId(Context.ConnectionId, user.UserName);
                NotifyUserStateChanged();
            }
            return error;
        }

        // Logout.
        public void Logout(string userName)
        {
            _userManager.Logout(userName);
            _userManager.RemoveConectionId(userName);
            NotifyUserStateChanged();
        }

        // Return the contacts list to all clients.
        public Dictionary<string, UserState> GetContactList()
        {
            return _userManager._contactList;
        }

        // Return the updated contects list to all clients.
        public void NotifyUserStateChanged()
        {
            Clients.All.notifyUserStateChanged(_userManager._contactList);
        }

        #endregion


        #region Chat request
        // Send chat request to second user.
        public void SendRequest(string reciverName, string senderName, bool isChat)
        {
            string conectionId = _userManager.GetConectionId(reciverName);
            Clients.Client(conectionId).InteractionRequest(senderName, isChat);
        }

        // Get the response from the second user.
        public void HandleInvitationResult(bool response, string senderName, string reciverName)
        {
            if (response)
            {
                ChangeUserStatus(reciverName, UserState.busy);
                //_gameManager.currentTurn = senderName;//?
                _userManager.AddNewPaier(senderName, reciverName);
            }
            string conectionId = _userManager.GetConectionId(senderName);
            Clients.Client(conectionId).getInvitationResult(response);

        }

        #endregion

        // Send message to second user.
        public void SendMessage(string message, string reciverName, string senderName)
        {
            string conectionId = _userManager.GetConectionId(reciverName);
            Clients.Client(conectionId).sendMessage(message, senderName);
        }


        // Change user state.
        public void ChangeUserStatus(string userName, UserState state)
        {
            _userManager.UpdateContactList(userName, state);
            NotifyUserStateChanged();
        }

        public void UserDisconnected(string userToChatWith)
        {
            _userManager.RemovePaier(userToChatWith);
            ChangeUserStatus(userToChatWith, UserState.online);

            string conectionId = _userManager.GetConectionId(userToChatWith);
            Clients.Client(conectionId).secondUserDisconnnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string currentUser = _userManager._userConections.FirstOrDefault((x) => x.Value == Context.ConnectionId).Key;
            ChangeUserStatus(currentUser, UserState.offline);
            string secondUser = _userManager.GetTheSecondUser(currentUser);
            if (secondUser != null)
            {
                ChangeUserStatus(secondUser, UserState.online);
                UserDisconnected(secondUser);
            }

            return base.OnDisconnected(stopCalled);
        }

        #region Game

        public void InitializeBoardGame(string senderName, string reciverName)
        {
            _gameManager.InitializeBoard(senderName, reciverName);
        }

        public string GetGameKey(string senderName, string ReciverName)
        {
            return _gameManager.GetGameKey(senderName, ReciverName);
        }

        public IGameBoardState GetGameBoard(string gameKey)
        {
            return _gameManager.GetGameBoard(gameKey);
        }

        public Dice RollDice(string gameKey)
        {
            Dice result = _gameManager.RollDice(gameKey);
            if (_gameManager._boards[gameKey].CurrentPlayer == _gameManager._boards[gameKey].BlackPlayer)
            {
                Clients.Client(_gameManager._boards[gameKey]._whiteConectionId).getDiceResult(result);
            }
            else
            {
                Clients.Client(_gameManager._boards[gameKey]._blackConectionId).getDiceResult(result);
            }
            return result;
        }

        public bool MoveChecker(int from, int to, string gameKey)
        {
            if (_gameManager.MoveChecker(from, to, gameKey))
            {
                Clients.Clients(_gameManager.GetConectionStrings(gameKey)).getUpdatedBoard((IGameBoardState)_gameManager._boards[gameKey]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool PrisonerCanEscape(string gameKey)
        {
            return _gameManager.PrisonerCanEscape(gameKey);
        }
        #endregion
    }
}