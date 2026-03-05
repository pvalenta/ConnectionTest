using System.Net.NetworkInformation;
using ConnectionTest.Services;

namespace ConnectionTest;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Network Router Latency Monitor");
        Console.WriteLine("===============================");
        Console.WriteLine();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n\nStopping monitoring...");
            Environment.Exit(0);
        };

        var networkInfoService = new NetworkInfoService();
        var gateway = networkInfoService.GetDefaultGateway();

        if (gateway == null)
        {
            Console.WriteLine("Error: No default gateway found!");
            return;
        }

        Console.WriteLine($"Target Router: {gateway}");
        Console.WriteLine();

        using var ping = new Ping();

        while (true)
        {
            try
            {
                var reply = await ping.SendPingAsync(gateway, 5000);
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var connectionInfo = networkInfoService.GetConnectionInfo();

                if (reply.Status == IPStatus.Success)
                {
                    Console.Write($"[{timestamp}] Latency: {reply.RoundtripTime,4} ms | {connectionInfo.Type,-4} | Local IP: {connectionInfo.LocalIpAddress,-15} | Router: {gateway}");

                    if (connectionInfo.Type == "WIFI")
                    {
                        var ssid = string.IsNullOrEmpty(connectionInfo.Ssid) ? "Not Available" : connectionInfo.Ssid;
                        var channel = connectionInfo.Channel > 0 ? connectionInfo.Channel.ToString() : "N/A";
                        Console.Write($" | SSID: {ssid,-20} | Channel: {channel,3} | {connectionInfo.FrequencyMhz} MHz | Signal: {connectionInfo.SignalQuality}%");
                    }

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"[{timestamp}] Ping failed: {reply.Status}");
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }
}
