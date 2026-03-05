using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ConnectionTest.Models;

namespace ConnectionTest.Services;

internal sealed class NetworkInfoService
{
    private readonly WifiInfoProvider _wifiInfoProvider = new();

    public IPAddress? GetDefaultGateway()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties()?.GatewayAddresses ?? Enumerable.Empty<GatewayIPAddressInformation>())
            .Select(g => g?.Address)
            .Where(a => a != null && !IPAddress.IsLoopback(a))
            .FirstOrDefault();
    }

    public ConnectionInfo GetConnectionInfo()
    {
        var activeInterface = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .FirstOrDefault(n => n.GetIPProperties().GatewayAddresses.Any());

        if (activeInterface == null)
        {
            return new ConnectionInfo { Type = "N/A" };
        }

        var ipProperties = activeInterface.GetIPProperties();
        string localIp = ipProperties.UnicastAddresses
            .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
            ?.Address.ToString() ?? "Unknown";

        if (activeInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
        {
            var wifiInfo = _wifiInfoProvider.GetWifiInfo();
            return new ConnectionInfo
            {
                Type = "WIFI",
                Ssid = wifiInfo.Ssid,
                Channel = wifiInfo.Channel,
                LocalIpAddress = localIp,
                FrequencyMhz = wifiInfo.FrequencyMhz,
                SignalQuality = wifiInfo.SignalQuality
            };
        }

        if (activeInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
        {
            return new ConnectionInfo
            {
                Type = "LAN",
                LocalIpAddress = localIp
            };
        }

        return new ConnectionInfo
        {
            Type = "OTHER",
            LocalIpAddress = localIp
        };
    }
}
