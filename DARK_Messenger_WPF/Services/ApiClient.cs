using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using DARK_Messenger_WPF.Models;

namespace DARK_Messenger_WPF.Services;

public static class ApiClient
{
    private static readonly HttpClient _http = new();
    private static string _baseUrl = (System.Environment.GetEnvironmentVariable("DARK_SERVER_URL") ?? "https://dark-messenger-0h3o.onrender.com") + "/api";
    public static string? Token { get; set; }
    public static string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value;
    }

    private static HttpRequestMessage Req(HttpMethod m, string p)
    {
        var r = new HttpRequestMessage(m, $"{_baseUrl}{p}");
        if (!string.IsNullOrEmpty(Token)) r.Headers.Add("Authorization", $"Bearer {Token}");
        return r;
    }

    private static async Task<HttpResponseMessage> SendRaw(HttpRequestMessage r)
    {
        return await _http.SendAsync(r);
    }

    private static async Task<string?> Send(HttpRequestMessage r)
    {
        using var res = await _http.SendAsync(r);
        return await res.Content.ReadAsStringAsync();
    }

    private static void MarkMine(Message m)
    {
        m.IsMine = m.SenderId == SettingsService.UserId;
    }

    private static async Task<List<T>?> GetList<T>(string path)
    {
        try
        {
            var b = await Send(Req(HttpMethod.Get, path));
            if (b == null) return null;
            return JsonConvert.DeserializeObject<List<T>>(b);
        }
        catch { return null; }
    }

    public static async Task<(bool ok, string error, string? token)> Register(string u, string dn, string pw)
    {
        try
        {
            var r = Req(HttpMethod.Post, "/auth/register");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { username = u, displayName = dn, password = pw }), Encoding.UTF8, "application/json");
            var res = await _http.SendAsync(r);
            var b = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                var j = JsonConvert.DeserializeAnonymousType(b, new { token = "", userId = 0 });
                return (true, "", j?.token);
            }
            var err = JsonConvert.DeserializeAnonymousType(b, new { message = "" });
            return (false, err?.message ?? "Ошибка регистрации", null);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public static async Task<(bool ok, string error, string? token, int userId, string? displayName)> Login(string u, string pw)
    {
        try
        {
            var r = Req(HttpMethod.Post, "/auth/login");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { username = u, password = pw }), Encoding.UTF8, "application/json");
            using var res = await _http.SendAsync(r);
            var b = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                var j = JsonConvert.DeserializeAnonymousType(b, new { token = "", userId = 0, displayName = "" });
                return (true, "", j?.token, j?.userId ?? 0, j?.displayName);
            }
            var err = JsonConvert.DeserializeAnonymousType(b, new { message = "" });
            return (false, err?.message ?? "Ошибка входа", null, 0, null);
        }
        catch (Exception ex) { return (false, ex.Message, null, 0, null); }
    }

    public static Task<List<Chat>?> GetChats() => GetList<Chat>("/chats");
    public static Task<List<Chat>?> GetGroups() => GetList<Chat>("/groups");

    public static async Task<int?> CreateChat(int targetUserId)
    {
        try
        {
            var r = Req(HttpMethod.Post, "/chats");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { userId = targetUserId }), Encoding.UTF8, "application/json");
            var b = await Send(r);
            if (b == null) return null;
            var j = JsonConvert.DeserializeAnonymousType(b, new { id = 0 });
            return j?.id;
        }
        catch { return null; }
    }

    public static async Task<bool> DeleteChat(int chatId)
    {
        try { using var r = await SendRaw(Req(HttpMethod.Delete, $"/chats/{chatId}")); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    public static async Task<int?> CreateGroup(string name, List<int> memberIds)
    {
        try
        {
            var r = Req(HttpMethod.Post, "/groups");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { name, memberIds }), Encoding.UTF8, "application/json");
            var b = await Send(r);
            if (b == null) return null;
            var j = JsonConvert.DeserializeAnonymousType(b, new { id = 0 });
            return j?.id;
        }
        catch { return null; }
    }

    public static async Task<List<Message>?> GetMessages(int chatId, int page = 1)
    {
        var msgs = await GetList<Message>($"/chats/{chatId}/messages?page={page}");
        if (msgs != null) foreach (var m in msgs) MarkMine(m);
        return msgs;
    }

    public static async Task<List<Message>?> GetGroupMessages(int groupId, int page = 1)
    {
        var msgs = await GetList<Message>($"/groups/{groupId}/messages?page={page}");
        if (msgs != null) foreach (var m in msgs) MarkMine(m);
        return msgs;
    }

    public static async Task<Message?> SendMessage(int chatId, string content, string? mediaUrl = null, string? mediaType = null, int? replyToId = null)
    {
        try
        {
            var r = Req(HttpMethod.Post, $"/chats/{chatId}/messages");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { content, mediaUrl, mediaType, replyToId }), Encoding.UTF8, "application/json");
            var b = await Send(r);
            if (b == null) return null;
            var m = JsonConvert.DeserializeObject<Message>(b);
            if (m != null) m.IsMine = true;
            return m;
        }
        catch { return null; }
    }

    public static async Task<Message?> SendGroupMessage(int groupId, string content, string? mediaUrl = null, string? mediaType = null, int? replyToId = null)
    {
        try
        {
            var r = Req(HttpMethod.Post, $"/groups/{groupId}/messages");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { content, mediaUrl, mediaType, replyToId }), Encoding.UTF8, "application/json");
            var b = await Send(r);
            if (b == null) return null;
            var m = JsonConvert.DeserializeObject<Message>(b);
            if (m != null) m.IsMine = true;
            return m;
        }
        catch { return null; }
    }

    public static async Task<bool> EditMessage(int chatId, int msgId, string content, bool isGroup = false)
    {
        try
        {
            var prefix = isGroup ? "/groups" : "/chats";
            var r = Req(HttpMethod.Put, $"{prefix}/{chatId}/messages/{msgId}");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { content }), Encoding.UTF8, "application/json");
            using var res = await SendRaw(r);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<bool> DeleteMessage(int chatId, int msgId, bool isGroup = false)
    {
        try
        {
            var prefix = isGroup ? "/groups" : "/chats";
            using var res = await SendRaw(Req(HttpMethod.Delete, $"{prefix}/{chatId}/messages/{msgId}"));
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<List<Message>?> SearchMessages(int chatId, string q)
    {
        var msgs = await GetList<Message>($"/chats/{chatId}/search?q={Uri.EscapeDataString(q)}");
        if (msgs != null) foreach (var m in msgs) MarkMine(m);
        return msgs;
    }

    public static async Task<List<Message>?> SearchGroupMessages(int groupId, string q)
    {
        var msgs = await GetList<Message>($"/groups/{groupId}/search?q={Uri.EscapeDataString(q)}");
        if (msgs != null) foreach (var m in msgs) MarkMine(m);
        return msgs;
    }

    public static async Task<List<Contact>> SearchUsers(string q)
    {
        try
        {
            var b = await Send(Req(HttpMethod.Get, $"/users/search?q={Uri.EscapeDataString(q)}"));
            if (b == null) return new();
            return JsonConvert.DeserializeObject<List<Contact>>(b) ?? new();
        }
        catch { return new(); }
    }

    public static async Task<(string? url, string? mediaType)?> UploadFile(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(fs), "file", Path.GetFileName(filePath));
            var r = Req(HttpMethod.Post, "/upload");
            r.Content = form;
            var b = await Send(r);
            if (b == null) return null;
            var j = JsonConvert.DeserializeAnonymousType(b, new { url = "", mediaType = "" });
            return (j?.url, j?.mediaType);
        }
        catch { return null; }
    }

    public static async Task<string?> UpdateProfile(string dn)
    {
        try
        {
            var r = Req(HttpMethod.Put, "/users/profile");
            r.Content = new StringContent(JsonConvert.SerializeObject(new { displayName = dn }), Encoding.UTF8, "application/json");
            using var res = await SendRaw(r);
            if (res.IsSuccessStatusCode) return dn;
            return null;
        }
        catch { return null; }
    }

    public static async Task<string?> UploadAvatar(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(fs), "avatar", Path.GetFileName(filePath));
            var r = Req(HttpMethod.Post, "/users/avatar");
            r.Content = form;
            var b = await Send(r);
            if (b == null) return null;
            var j = JsonConvert.DeserializeAnonymousType(b, new { avatarUrl = "" });
            return j?.avatarUrl;
        }
        catch { return null; }
    }

    public static Task<List<Contact>?> GetGroupMembers(int groupId)
    {
        return GetList<Contact>($"/groups/{groupId}/members");
    }

    public static async Task<List<Session>?> GetSessions()
    {
        try { return await GetList<Session>("/auth/sessions"); }
        catch { return null; }
    }

    public static async Task<bool> DeleteSession(int sessionId)
    {
        try
        {
            var req = Req(HttpMethod.Delete, $"/auth/sessions/{sessionId}");
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

}
