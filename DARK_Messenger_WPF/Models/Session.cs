using Newtonsoft.Json;

namespace DARK_Messenger_WPF.Models;

public class Session
{
    public int Id { get; set; }
    [JsonProperty("device_name")]
    public string DeviceName { get; set; } = "";
    public string Ip { get; set; } = "";
    [JsonProperty("created_at")]
    public string CreatedAt { get; set; } = "";
    [JsonProperty("last_active")]
    public string LastActive { get; set; } = "";
}
