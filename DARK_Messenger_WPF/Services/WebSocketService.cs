using System.IO;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DARK_Messenger_WPF.Models;

namespace DARK_Messenger_WPF.Services;

public static class WebSocketService
{
    private static ClientWebSocket? _ws;
    private static CancellationTokenSource? _cts;
    private static bool _connected;
    private static string? _lastToken;
    private static readonly object _reconnectLock = new();
    private static bool _reconnecting;

    public static bool IsConnected => _connected;

    public static event Action<int, bool>? OnOnlineStatus;
    public static event Action<int, Message>? OnNewMessage;
    public static event Action<int, Message>? OnNewGroupMessage;
    public static event Action<int, int, string>? OnEditMessage;
    public static event Action<int, int>? OnDeleteMessage;
    public static event Action<int, int, bool>? OnTyping;
    public static event Action<bool>? OnConnectionChanged;

    public static async Task Connect(string token)
    {
        _lastToken = token;
        await Disconnect();
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();
        try
        {
            var serverUrl = ApiClient.BaseUrl.Replace("https://", "wss://").Replace("http://", "ws://").Replace("/api", "");
            await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
            var authMsg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "auth", token }));
            await _ws.SendAsync(new ArraySegment<byte>(authMsg), WebSocketMessageType.Text, true, _cts.Token);
            _connected = true;
            OnConnectionChanged?.Invoke(true);
            _ = ReceiveLoop();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WS Connect error: {ex.Message}");
            _connected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }

    public static async Task Disconnect()
    {
        _connected = false;
        OnConnectionChanged?.Invoke(false);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        if (_ws?.State == WebSocketState.Open)
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
        _ws?.Dispose();
        _ws = null;
    }

    public static async Task Reconnect()
    {
        lock (_reconnectLock)
        {
            if (_reconnecting) return;
            _reconnecting = true;
        }
        try
        {
            if (!string.IsNullOrEmpty(_lastToken))
                await Connect(_lastToken);
        }
        finally
        {
            lock (_reconnectLock) { _reconnecting = false; }
        }
    }

    public static async Task SendTyping(int chatId, bool typing, bool isGroup = false)
    {
        if (_ws?.State != WebSocketState.Open) return;
        try
        {
            object payload = isGroup
                ? new { type = typing ? "typing" : "stop_typing", groupId = chatId }
                : new { type = typing ? "typing" : "stop_typing", chatId };
            var msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            await _ws.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
        }
        catch { }
    }

    private static async Task ReceiveLoop()
    {
        var buffer = new byte[102400];
        try
        {
            while (_ws?.State == WebSocketState.Open)
            {
                var cts = _cts;
                if (cts == null || cts.IsCancellationRequested) break;

                var segment = new ArraySegment<byte>(buffer);
                var result = await _ws.ReceiveAsync(segment, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                int count = result.Count;
                if (result.EndOfMessage == false)
                {
                    using var ms = new MemoryStream();
                    ms.Write(buffer, 0, count);
                    while (!result.EndOfMessage)
                    {
                        result = await _ws.ReceiveAsync(segment, cts.Token);
                        ms.Write(buffer, 0, result.Count);
                        count += result.Count;
                    }
                    var fullJson = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
                    ProcessMessage(fullJson);
                }
                else
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, count);
                    ProcessMessage(json);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WS receive error: {ex.Message}");
        }

        _connected = false;
        OnConnectionChanged?.Invoke(false);

        if (_cts != null && !_cts.IsCancellationRequested && _lastToken != null)
        {
            await Task.Delay(3000);
            await Reconnect();
        }
    }

    private static void ProcessMessage(string json)
    {
        try
        {
            var msg = JObject.Parse(json);
            var type = msg.Value<string>("type");

            if (type == "online")
            {
                var uid = msg.Value<int>("userId");
                var online = msg.Value<bool>("online");
                OnOnlineStatus?.Invoke(uid, online);
            }
            else if (type == "message")
            {
                var m = msg["message"]?.ToString();
                if (m != null)
                {
                    var message = JsonConvert.DeserializeObject<Message>(m);
                    if (message != null)
                    {
                        message.IsMine = message.SenderId == SettingsService.UserId;
                        var chatId = msg.Value<int>("chatId");
                        OnNewMessage?.Invoke(chatId, message);
                    }
                }
            }
            else if (type == "group_message")
            {
                var m = msg["message"]?.ToString();
                if (m != null)
                {
                    var message = JsonConvert.DeserializeObject<Message>(m);
                    if (message != null)
                    {
                        message.IsMine = message.SenderId == SettingsService.UserId;
                        var groupId = msg.Value<int>("groupId");
                        OnNewGroupMessage?.Invoke(groupId, message);
                    }
                }
            }
            else if (type == "edit")
            {
                var chatId = msg.Value<int>("chatId");
                if (chatId == 0) chatId = msg.Value<int>("groupId");
                var messageObj = msg["message"];
                if (messageObj != null)
                {
                    var msgId = messageObj.Value<int>("id");
                    var content = messageObj.Value<string>("content") ?? "";
                    OnEditMessage?.Invoke(chatId, msgId, content);
                }
            }
            else if (type == "delete")
            {
                var chatId = msg.Value<int>("chatId");
                if (chatId == 0) chatId = msg.Value<int>("groupId");
                var msgId = msg.Value<int>("messageId");
                OnDeleteMessage?.Invoke(chatId, msgId);
            }
            else if (type == "typing")
            {
                var chatId = msg.Value<int>("chatId");
                var uid = msg.Value<int>("userId");
                var isTyping = msg.Value<bool>("isTyping");
                OnTyping?.Invoke(chatId, uid, isTyping);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WS ProcessMessage error: {ex.Message} | Data: {json?.Substring(0, Math.Min(json.Length, 200))}");
        }
    }
}
