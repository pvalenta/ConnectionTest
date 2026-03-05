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

## Root cause fixed

A previous implementation attempted to read a non-existent `ulFrequency` field from `WLAN_ASSOCIATION_ATTRIBUTES`.
That structure does not expose frequency, causing invalid values (`0` / `N/A`).

Fix:

- Corrected `WLAN_ASSOCIATION_ATTRIBUTES` layout
- Added channel query via `wlan_intf_opcode_channel_number`
- Added BSS lookup to obtain actual center frequency

## Run

```powershell
dotnet run
```

Press `Ctrl+C` to stop.
