﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Message;

namespace UserBayraktarServer
{
    public class UserConnection
    {
        private TcpClient _client;
        private NetworkStream _stream => _client.GetStream();

        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _token;
        public UserConnection(TcpClient client)
        {
            _client = client;
            _isRun = true;
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
        }

        private bool _isRun;
        public Task RunAsync()=>Task.Factory.StartNew(Run, _token);

        public void Run()
        {
            while (_isRun)
            {
                Receive();
            }
        }

        public Action<UserConnection, MessagePacket> Query;
        private void Receive()
        {
            var message = _read();
            if (message == null) return;
            Query?.Invoke(this, message);
        }

        public void Close()
        {
            MessageCommand command= new MessageCommand();
            command.Command = "Disconnect";
            Send(command);
            _cts.Cancel();
            _isRun = false;
            _client.Close();
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
                } while (_stream.DataAvailable);
                return MessagePacket.FromBytes(stream.ToArray());
            }
        }

        public void Send(MessagePacket message)
        {
            var buffer = message.ToBytes();
            _stream.Write(buffer, 0, buffer.Length);
        }
        public MessageAuthorize Authorize()
        {
            if (_read() is MessageAuthorize authorize)
            {
                return authorize;
            }
            return null;
        }
    }
}