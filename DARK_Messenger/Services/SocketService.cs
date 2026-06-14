using System.Text.Json;
using System.Net.WebSockets;
using DARK_Messenger.Models;

namespace DARK_Messenger.Services;

public class SocketService
{
    private ClientWebSocket? _ws;
    private string? _token;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    public event Action<Message>? OnMessageReceived;
    public event Action<string, string>? OnTyping; // chatId, userId
    public event Action<string, bool>? OnUserStatus; // userId, isOnline

    public void Connect(string token)
    {
        _token = token;
        _cts = new CancellationTokenSource();
        _ = Task.Run(ConnectAsync, _cts.Token);
    }

    private async Task ConnectAsync()
    {
        try
        {
            _ws = new ClientWebSocket();
            var wsBase = ApiClient.BaseUrl.Replace("https://", "wss://").Replace("http://", "ws://");
            var uri = new Uri($"{wsBase}/ws?token={_token}");
            await _ws.ConnectAsync(uri, _cts.Token);
            _receiveTask = ReceiveLoopAsync(_cts.Token);
            await _receiveTask;
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(json);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(1000, ct); }
        }
    }

    private void HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "message":
                    var msg = JsonSerializer.Deserialize<Message>(json);
                    if (msg != null) OnMessageReceived?.Invoke(msg);
                    break;
                case "typing":
                    var chatId = root.GetProperty("chat_id").GetString() ?? "";
                    var userId = root.GetProperty("user_id").GetString() ?? "";
                    OnTyping?.Invoke(chatId, userId);
                    break;
                case "status":
                    var statusUserId = root.GetProperty("user_id").GetString() ?? "";
                    var online = root.GetProperty("online").GetBoolean();
                    OnUserStatus?.Invoke(statusUserId, online);
                    break;
            }
        }
        catch { }
    }

    public async Task SendMessageAsync(Message message)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(new { type = "message", message.Id, message.ChatId, message.SenderId, message.SenderName, message.Type, message.Content, message.FileUrl, message.FileName, message.FileSize, message.ThumbnailUrl, message.Duration, message.Timestamp });
        await _ws.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SendTypingAsync(string chatId)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(new { type = "typing", chat_id = chatId });
        await _ws.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SendReadAsync(string chatId, string messageId)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(new { type = "read", chat_id = chatId, message_id = messageId });
        await _ws.SendAsync(System.Text.Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait(1000);
        _ws?.Dispose();
        _ws = null;
    }
}
