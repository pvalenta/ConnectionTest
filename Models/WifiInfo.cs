namespace ConnectionTest.Models;

internal sealed class WifiInfo
{
    public string Ssid { get; set; } = "";
    public int Channel { get; set; }
    public uint FrequencyMhz { get; set; }
    public uint SignalQuality { get; set; }
}
