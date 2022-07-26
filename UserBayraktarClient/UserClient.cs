﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BayraktarClient;
using BayraktarGame;
using Message;

namespace UserGameClient
{
    public class UserClient 
    {
        private User _user;
        public User User
        {
            get => _user;
            set
            {
                if (IsConnected != true)
                    _user = value;
                else
                {
                    throw new InvalidOperationException("User is connected and can't be changed");
                }
            }
        }
        private IPAddress _serverIp;
        private int _serverPort;
        private TcpClient _client;
        
        private NetworkStream _stream => _client?.GetStream();
        public bool? IsConnected => _client?.Connected;
        public UserClient(string serverIp, int serverPort, User user) : this(serverIp, serverPort)
        {
            User = user;
        }

        public UserClient(string serverIp, int serverPort) : this(IPAddress.Parse(serverIp), serverPort)
        {
        }

        private CancellationTokenSource _cts;
        private CancellationToken _token;
        public UserClient(IPAddress serverIp, int serverPort)
        {
            _client = new TcpClient();
            _serverIp = serverIp;
            _serverPort = serverPort;
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
        }
        public event Action<string> Info;
        public event Action<bool> Connected;

        public GameClient GameConnection;
        public Action<GameClient, GameRole> StartGame;
        public Action<bool> WaitForGame;
        public void StartSingleGame()
        {
            SendCommand("SINGLE");
            WaitForGame?.Invoke(true);
            
        }

        public void StartMultiGame()
        {
            SendCommand("MULTI");
            WaitForGame?.Invoke(true);
            
        }
        

        private void _startGame(MessageGameData data)
        {
            GameConnection = new GameClient(User, 1000, data.Server)
            {
                Units = data.Units,
                Role = data.GameRole
            };
            GameConnection.GameOver += GameOver;
            WaitForGame?.Invoke(false);
            StartGame?.Invoke(GameConnection, data.GameRole);
        }
        private void GameOver(GameClient client)
        {

            MessageGameResult result = new MessageGameResult { User = client.User, Score = client.Role==GameRole.Defense?client.ScoreDefense:client.ScoreAttack , Server = client.Server};
            Send(result);
        }

        private async void _connect(Func<bool> authorization)
        {
            if (_client == null)
                _client = new TcpClient();
            if(IsConnected==true)
                return;
            
            try
            {
                if (User == null)
                    throw new ArgumentNullException("No user to authtorize");
                await _client.ConnectAsync(_serverIp, _serverPort);

                var auth = authorization();
                Connected?.Invoke(auth);
                if (!auth)
                {
                    Info?.Invoke("Authorization false");
                    Close();
                    _client = new TcpClient();
                }

                await Task.Run(_start, _token);
            }
            catch (Exception e)
            {
                Connected?.Invoke(false);
                Info?.Invoke(e.Message);
            }
        }
        
        
        public Task ConnectAsync() => Task.Factory.StartNew(Connect, _token);
        public void Connect()
        {
            _connect(() => _authorize(true));
        }

        //R = Registration
        public void ConnectR()
        {
            _connect(() => _authorize(false));
        }
        public Task ConnectAsyncR() => Task.Factory.StartNew(ConnectR, _token);
  
        public void Close()
        {
            if (IsConnected != true)
                return;
            try
            {
                Send(new MessageCommand { Command = "DISCONNECTED" });
            }
            finally
            {
                _client?.Close();
                _client = null;

                Connected?.Invoke(false);
            }
        }
        private bool _authorize(bool isLogin)
        {
            var mode = isLogin ? AuthorizeMode.Login : AuthorizeMode.Registration;
            var authorize = new MessageAuthorize(mode)
            {
                Login = User.Login,
                Password = User.PassWord
            };
            Send(authorize);
            if (_read() is MessageBool response)
            {
                return response.Response;
            }
            return false;
        }

        private MessagePacket _read()
        {
            var buffer = new byte[2048];
            using (var stream = new MemoryStream())
            {
                do
                {
                    var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, bytesRead);
                    Thread.Sleep(50);
                } while (_stream.DataAvailable);
                return MessagePacket.FromBytes(stream.ToArray());
            }

        }
        private bool _isRun = false;
        private void _start()
        {
            _isRun = true;
            while (_isRun)
            {
                try
                {
                    var message = _read();
                    _handle(message);
                }
                catch (Exception e)
                {
                    _isRun= false;
                    Disconnect();
                }
            }
        }

        public Action<List<Statistic>> GetRating;
        public Action Disconnect;
        private void _handle(MessagePacket message)
        {
            if(message == null)
                return;
            switch (message)
            {
                case MessageDataContent content:
                    if (content.Description.Equals("RATING"))
                    {
                        GetRating?.Invoke(_loadRating(content.Content));
                    }
                    break;
                case MessageGameData data:
                    _startGame(data);
                    break;
                case MessageCommand command:
                    switch (command.Command)
                    {
                        case "DISCONNECT":
                            Disconnect?.Invoke();
                            Close();
                            break;
                    }
                    break;
            }
        }
        private List<Statistic> _loadRating(byte[] dataContent)
        {
            using (MemoryStream stream = new MemoryStream(dataContent))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                if (formatter.Deserialize(stream) is List<Statistic> statistics)
                {
                    return statistics;
                }
                return null;
            }
        }
       
        

      
        public void Send(MessagePacket message)
        {
            var buffer = message.ToBytes();
            _stream.Write(buffer, 0, buffer.Length);
        }
        public void SendCommand(string command)
        {
            var message = new MessageCommand
            {
                Command = command
            };
            Send(message);
        }
        public void Refresh()
        {
            _client = new TcpClient();
        }
        

        

        public void StopWaitingForGame()
        {
            SendCommand("STOP_WAITING");
        }
    }
}
