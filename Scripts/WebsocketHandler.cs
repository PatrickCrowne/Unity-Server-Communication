using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityServerCommunication
{
    public static class WebsocketHandler
    {

        private const string WebsocketProtocol = "ws://";
        private const string SecureWebsocketProtocol = "wss://";
        private const string DebugHeader = "[WebsocketHandler] - ";

        private static string _currentAddress;
        private static ClientWebSocket _socket;
        
        private static Queue<string> _outgoingMessages;
        
        /// <summary>
        /// Connects to a websocket with the given address
        /// </summary>
        /// <param name="address">The internet address of the websocket to connect to.</param>
        /// <param name="secure">Determines if this websocket uses a secure connection or not.</param>
        /// <param name="onSuccess">Function to execute upon successfully connecting to the websocket.</param>
        /// <param name="onFailure">Function to execute if the connection to the websocket failed.</param>
        public static async void Connect(string address, bool secure = false, Action onSuccess = null, Action onFailure = null)
        {

            // Disconnect if we are already connected.
            await Disconnect();

            // Determine protocol to use
            string protocol = secure ? SecureWebsocketProtocol : WebsocketProtocol;
            
            _currentAddress = address;
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(new Uri(protocol + _currentAddress + "/"), CancellationToken.None);
            
            switch (_socket.State)
            {
                case WebSocketState.Open:
                    Debug.Log(DebugHeader + "Web Socket Connected.");
                    _SocketSend();
                    _SocketReceive();
                    onSuccess?.Invoke();
                    return;
                default:
                    Debug.Log(DebugHeader + "Web Socket Connection Failed!");
                    break;
            }
            
            onFailure?.Invoke();
            
        }
        
        /// <summary>
        /// Handles the disconnecting of a currently open websocket.
        /// </summary>
        public static async Task Disconnect()
        {
            if (_socket is { State: WebSocketState.Open }) await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        
        /// <summary>
        /// Sends any queued messages to the currently open websocket, this function is a loop that will run while the
        /// websocket is open.
        /// </summary>
        private static async void _SocketSend()
        {
            while (_socket.State == WebSocketState.Open)
            {
                while (_outgoingMessages.Count == 0) await Task.Yield();
                string message = _outgoingMessages.Dequeue();
                if (message == "close")
                {
                    await Disconnect();
                    Console.WriteLine("Web Socket Closed.");
                    break;
                }
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                Debug.Log(DebugHeader + "Web Socket Sent\n" + message);
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        
        /// <summary>
        /// Receives all messages sent to the client from the open websocket, this function is a loop that will run
        /// while the websocket is open.
        /// </summary>
        private static async void _SocketReceive()
        {
            byte[] buffer = new byte[1024];
            while (_socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await Disconnect();
                        Debug.Log(DebugHeader + "Web Socket Closed.");
                        break;
                    case WebSocketMessageType.Text:
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log(DebugHeader + "Web Socket Received\n" + message);
                        break;
                }
            }
        }
        
    }
}