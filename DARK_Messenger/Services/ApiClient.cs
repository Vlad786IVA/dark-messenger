using System.Net.Http.Json;
using System.Text.Json;
using DARK_Messenger.Models;
using ContactModel = DARK_Messenger.Models.Contact;

namespace DARK_Messenger.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _token;
    public string? Token => _token;
    private static readonly string BaseUrl = System.Environment.GetEnvironmentVariable("DARK_SERVER_URL") ?? "http://10.0.2.2:8080";

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    }

    public void SetToken(string token) => _token = token;
    public void ClearToken() => _token = null;

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? content = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (_token != null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        if (content != null)
            request.Content = JsonContent.Create(content);
        return request;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var request = CreateRequest(HttpMethod.Get, url);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostAsync<T>(string url, object data)
    {
        var request = CreateRequest(HttpMethod.Post, url, data);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<FileUploadResponse?> UploadFileAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", fileName);

        var request = CreateRequest(HttpMethod.Post, "/api/upload");
        request.Content = form;
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FileUploadResponse>();
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "/api/auth/login", new { username, password });
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new Exception(err?.Message ?? "Ошибка входа");
            }
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось подключиться к серверу");
        }
    }

    public async Task<RegisterResponse?> RegisterAsync(string username, string displayName, string password)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Post, "/api/auth/register", new { username, displayName, password });
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new Exception(err?.Message ?? "Ошибка регистрации");
            }
            return await response.Content.ReadFromJsonAsync<RegisterResponse>();
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось подключиться к серверу");
        }
    }

    public async Task<List<Chat>?> GetChatsAsync()
        => await GetAsync<List<Chat>>("/api/chats");

    public async Task<string?> CreateChatAsync(string userId)
    {
        var result = await PostAsync<CreateChatResponse>("/api/chats", new { user_id = userId });
        return result?.Id;
    }

    public async Task<List<Message>?> GetMessagesAsync(string chatId)
        => await GetAsync<List<Message>>($"/api/messages/{chatId}");

    public async Task<SendMessageResponse?> SendMessageAsync(string chatId, object message)
        => await PostAsync<SendMessageResponse>($"/api/messages/{chatId}", message);

    public async Task<List<ContactModel>?> GetContactsAsync()
        => await GetAsync<List<ContactModel>>("/api/contacts");

    public async Task<bool> AddContactAsync(string userId)
    {
        var result = await PostAsync<dynamic>("/api/contacts", new { user_id = userId });
        return result != null;
    }

    public async Task<List<ContactModel>?> SearchUsersAsync(string query)
        => await GetAsync<List<ContactModel>>($"/api/users/search?q={Uri.EscapeDataString(query)}");
}

public class LoginResponse { public string Token { get; set; } = ""; public string UserId { get; set; } = ""; public string DisplayName { get; set; } = ""; public string Username { get; set; } = ""; }
public class RegisterResponse { public string Token { get; set; } = ""; public string UserId { get; set; } = ""; }
public class CreateChatResponse { public string Id { get; set; } = ""; }
public class SendMessageResponse { public string Id { get; set; } = ""; }
public class FileUploadResponse { public string FileUrl { get; set; } = ""; public string FileName { get; set; } = ""; public long FileSize { get; set; } }
public class ErrorResponse { public string Message { get; set; } = ""; }
