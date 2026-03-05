namespace ConnectionTest.Models;

internal sealed class ConnectionInfo
{
    public string Type { get; set; } = "";
    public string Ssid { get; set; } = "";
    public int Channel { get; set; }
    public string LocalIpAddress { get; set; } = "";
    public uint FrequencyMhz { get; set; }
    public uint SignalQuality { get; set; }
}
