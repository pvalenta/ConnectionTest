using System.Runtime.InteropServices;
using System.Text;
using ConnectionTest.Models;
using ConnectionTest.Native;

namespace ConnectionTest.Services;

internal sealed class WifiInfoProvider
{
    public WifiInfo GetWifiInfo()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WifiInfo { Ssid = "N/A (Non-Windows)", Channel = 0 };
        }

        IntPtr clientHandle = IntPtr.Zero;
        IntPtr interfaceListPtr = IntPtr.Zero;
        IntPtr dataPtr = IntPtr.Zero;

        try
        {
            uint result = WlanNative.WlanOpenHandle(2, IntPtr.Zero, out _, out clientHandle);
            if (result != 0)
            {
                return new WifiInfo { Ssid = $"WlanOpenHandle failed: {result}", Channel = 0 };
            }

            result = WlanNative.WlanEnumInterfaces(clientHandle, IntPtr.Zero, out interfaceListPtr);
            if (result != 0)
            {
                return new WifiInfo { Ssid = $"WlanEnumInterfaces failed: {result}", Channel = 0 };
            }

            var interfaceList = Marshal.PtrToStructure<WlanNative.WLAN_INTERFACE_INFO_LIST>(interfaceListPtr);
            if (interfaceList.dwNumberOfItems == 0)
            {
                return new WifiInfo { Ssid = "No WLAN interfaces", Channel = 0 };
            }

            int interfaceInfoSize = Marshal.SizeOf<WlanNative.WLAN_INTERFACE_INFO>();
            IntPtr interfacePtr = IntPtr.Add(interfaceListPtr, 8);

            for (int i = 0; i < interfaceList.dwNumberOfItems; i++)
            {
                var interfaceInfo = Marshal.PtrToStructure<WlanNative.WLAN_INTERFACE_INFO>(interfacePtr);

                if (interfaceInfo.isState == 1)
                {
                    result = WlanNative.WlanQueryInterface(
                        clientHandle,
                        ref interfaceInfo.InterfaceGuid,
                        WlanNative.WLAN_INTF_OPCODE.wlan_intf_opcode_current_connection,
                        IntPtr.Zero,
                        out _,
                        out dataPtr,
                        out _);

                    if (result == 0 && dataPtr != IntPtr.Zero)
                    {
                        var connAttr = Marshal.PtrToStructure<WlanNative.WLAN_CONNECTION_ATTRIBUTES>(dataPtr);
                        int ssidLength = (int)connAttr.wlanAssociationAttributes.dot11Ssid.uSSIDLength;

                        if (ssidLength > 0 && ssidLength <= 32)
                        {
                            byte[] ssidBytes = new byte[ssidLength];
                            Array.Copy(connAttr.wlanAssociationAttributes.dot11Ssid.ucSSID, ssidBytes, ssidLength);

                            string ssid = Encoding.UTF8.GetString(ssidBytes);
                            uint signalQuality = connAttr.wlanAssociationAttributes.wlanSignalQuality;

                            uint channel = GetChannelNumber(clientHandle, interfaceInfo.InterfaceGuid);
                            uint frequencyKhz = GetCenterFrequencyKhz(clientHandle, interfaceInfo.InterfaceGuid, connAttr.wlanAssociationAttributes.dot11Bssid);

                            if (channel == 0 && frequencyKhz > 0)
                            {
                                channel = (uint)GetChannelFromFrequency(frequencyKhz);
                            }

                            uint frequencyMhz = frequencyKhz > 0
                                ? frequencyKhz / 1000
                                : GetFrequencyFromChannel(channel);

                            WlanNative.WlanFreeMemory(dataPtr);
                            dataPtr = IntPtr.Zero;

                            return new WifiInfo
                            {
                                Ssid = ssid,
                                Channel = (int)channel,
                                FrequencyMhz = frequencyMhz,
                                SignalQuality = signalQuality
                            };
                        }

                        WlanNative.WlanFreeMemory(dataPtr);
                        dataPtr = IntPtr.Zero;
                    }
                }

                interfacePtr = IntPtr.Add(interfacePtr, interfaceInfoSize);
            }

            return new WifiInfo { Ssid = "Not connected", Channel = 0 };
        }
        catch (Exception ex)
        {
            return new WifiInfo { Ssid = $"Error: {ex.Message}", Channel = 0 };
        }
        finally
        {
            if (dataPtr != IntPtr.Zero)
            {
                WlanNative.WlanFreeMemory(dataPtr);
            }

            if (interfaceListPtr != IntPtr.Zero)
            {
                WlanNative.WlanFreeMemory(interfaceListPtr);
            }

            if (clientHandle != IntPtr.Zero)
            {
                WlanNative.WlanCloseHandle(clientHandle, IntPtr.Zero);
            }
        }
    }

    private static uint GetChannelNumber(IntPtr clientHandle, Guid interfaceGuid)
    {
        IntPtr channelPtr = IntPtr.Zero;

        try
        {
            uint result = WlanNative.WlanQueryInterface(
                clientHandle,
                ref interfaceGuid,
                WlanNative.WLAN_INTF_OPCODE.wlan_intf_opcode_channel_number,
                IntPtr.Zero,
                out _,
                out channelPtr,
                out _);

            if (result != 0 || channelPtr == IntPtr.Zero)
            {
                return 0;
            }

            return (uint)Marshal.ReadInt32(channelPtr);
        }
        finally
        {
            if (channelPtr != IntPtr.Zero)
            {
                WlanNative.WlanFreeMemory(channelPtr);
            }
        }
    }

    private static uint GetCenterFrequencyKhz(IntPtr clientHandle, Guid interfaceGuid, byte[] currentBssid)
    {
        IntPtr bssListPtr = IntPtr.Zero;

        try
        {
            uint result = WlanNative.WlanGetNetworkBssList(
                clientHandle,
                ref interfaceGuid,
                IntPtr.Zero,
                WlanNative.DOT11_BSS_TYPE.dot11_BSS_type_any,
                false,
                IntPtr.Zero,
                out bssListPtr);

            if (result != 0 || bssListPtr == IntPtr.Zero)
            {
                return 0;
            }

            var bssList = Marshal.PtrToStructure<WlanNative.WLAN_BSS_LIST>(bssListPtr);
            int entrySize = Marshal.SizeOf<WlanNative.WLAN_BSS_ENTRY>();
            IntPtr entryPtr = IntPtr.Add(bssListPtr, 8);

            for (int i = 0; i < bssList.dwNumberOfItems; i++)
            {
                var bssEntry = Marshal.PtrToStructure<WlanNative.WLAN_BSS_ENTRY>(entryPtr);

                if (bssEntry.dot11Bssid is { Length: 6 } && currentBssid is { Length: 6 } && bssEntry.dot11Bssid.SequenceEqual(currentBssid))
                {
                    return bssEntry.ulChCenterFrequency;
                }

                entryPtr = IntPtr.Add(entryPtr, entrySize);
            }

            return 0;
        }
        finally
        {
            if (bssListPtr != IntPtr.Zero)
            {
                WlanNative.WlanFreeMemory(bssListPtr);
            }
        }
    }

    private static int GetChannelFromFrequency(uint frequencyKhz)
    {
        uint frequencyMhz = frequencyKhz / 1000;

        if (frequencyMhz >= 2412 && frequencyMhz <= 2484)
        {
            return frequencyMhz == 2484 ? 14 : (int)((frequencyMhz - 2412) / 5) + 1;
        }

        if (frequencyMhz >= 5170 && frequencyMhz <= 5250)
        {
            return (int)((frequencyMhz - 5170) / 5) + 34;
        }

        if (frequencyMhz >= 5250 && frequencyMhz <= 5330)
        {
            return (int)((frequencyMhz - 5250) / 5) + 50;
        }

        if (frequencyMhz >= 5490 && frequencyMhz <= 5730)
        {
            return (int)((frequencyMhz - 5490) / 5) + 98;
        }

        if (frequencyMhz >= 5735 && frequencyMhz <= 5835)
        {
            return (int)((frequencyMhz - 5735) / 5) + 149;
        }

        if (frequencyMhz >= 5955 && frequencyMhz <= 7115)
        {
            return (int)((frequencyMhz - 5955) / 5) + 1;
        }

        return frequencyMhz > 0 ? (int)frequencyMhz : 0;
    }

    private static uint GetFrequencyFromChannel(uint channel)
    {
        if (channel == 0)
        {
            return 0;
        }

        if (channel == 14)
        {
            return 2484;
        }

        if (channel >= 1 && channel <= 13)
        {
            return 2412 + ((channel - 1) * 5);
        }

        if (channel >= 32 && channel <= 177)
        {
            return 5000 + (channel * 5);
        }

        if (channel >= 1 && channel <= 233)
        {
            return 5950 + (channel * 5);
        }

        return 0;
    }
}
