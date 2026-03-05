# ConnectionTest

`ConnectionTest` is a .NET 10 console app that monitors router latency and prints live network details every second.

## What it shows

- Ping latency to the default gateway
- Connection type (`WIFI`, `LAN`, `OTHER`)
- Local IPv4 address
- For Wi-Fi:
  - SSID
  - Channel
  - Frequency (MHz)
  - Signal quality (%)

## How it works

- `Program.cs` runs a continuous ping loop and prints output.
- `Services/NetworkInfoService.cs` detects active interface and gateway.
- `Services/WifiInfoProvider.cs` reads Wi-Fi metrics using Windows WLAN APIs.
- `Native/WlanNative.cs` contains P/Invoke definitions for `wlanapi.dll`.

## Windows Wi-Fi internals used

The app targets Windows 11 and uses native WLAN APIs:

- `WlanOpenHandle`
- `WlanEnumInterfaces`
- `WlanQueryInterface`
  - `wlan_intf_opcode_current_connection`
  - `wlan_intf_opcode_channel_number`
- `WlanGetNetworkBssList`
- `WlanFreeMemory`

Frequency is read from `WLAN_BSS_ENTRY.ulChCenterFrequency` (kHz), then converted to MHz.

## Sample output

### Wi-Fi connection

```text
Network Router Latency Monitor
===============================

Target Router: 192.168.1.1

[14:22:31] Latency:    5 ms | WIFI | Local IP: 192.168.1.120  | Router: 192.168.1.1 | SSID: HomeNetwork          | Channel:  36 | 5180 MHz | Signal: 91%
[14:22:32] Latency:    4 ms | WIFI | Local IP: 192.168.1.120  | Router: 192.168.1.1 | SSID: HomeNetwork          | Channel:  36 | 5180 MHz | Signal: 90%
```

### LAN connection

```text
Network Router Latency Monitor
===============================

Target Router: 10.0.0.1

[09:05:11] Latency:    1 ms | LAN  | Local IP: 10.0.0.25      | Router: 10.0.0.1
[09:05:12] Latency:    1 ms | LAN  | Local IP: 10.0.0.25      | Router: 10.0.0.1
```

## Run

```powershell
dotnet run
```

Press `Ctrl+C` to stop.
