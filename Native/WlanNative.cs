using System.Runtime.InteropServices;

namespace ConnectionTest.Native;

internal static class WlanNative
{
    [DllImport("wlanapi.dll")]
    public static extern uint WlanOpenHandle(uint clientVersion, IntPtr pReserved, out uint negotiatedVersion, out IntPtr clientHandle);

    [DllImport("wlanapi.dll")]
    public static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    public static extern uint WlanEnumInterfaces(IntPtr clientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll")]
    public static extern uint WlanQueryInterface(
        IntPtr clientHandle,
        ref Guid interfaceGuid,
        WLAN_INTF_OPCODE opCode,
        IntPtr pReserved,
        out uint dataSize,
        out IntPtr ppData,
        out int wlanOpcodeValueType);

    [DllImport("wlanapi.dll")]
    public static extern uint WlanGetNetworkBssList(
        IntPtr clientHandle,
        ref Guid interfaceGuid,
        IntPtr dot11SsidIntPtr,
        DOT11_BSS_TYPE dot11BssType,
        [MarshalAs(UnmanagedType.Bool)] bool securityEnabled,
        IntPtr reservedPtr,
        out IntPtr wlanBssListPtr);

    [DllImport("wlanapi.dll")]
    public static extern void WlanFreeMemory(IntPtr pMemory);

    public enum WLAN_INTF_OPCODE
    {
        wlan_intf_opcode_current_connection = 7,
        wlan_intf_opcode_channel_number = 8
    }

    public enum DOT11_BSS_TYPE
    {
        dot11_BSS_type_infrastructure = 1,
        dot11_BSS_type_independent = 2,
        dot11_BSS_type_any = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WLAN_INTERFACE_INFO_LIST
    {
        public uint dwNumberOfItems;
        public uint dwIndex;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WLAN_INTERFACE_INFO
    {
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strInterfaceDescription;

        public int isState;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WLAN_CONNECTION_ATTRIBUTES
    {
        public int isState;
        public int wlanConnectionMode;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strProfileName;

        public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
        public WLAN_SECURITY_ATTRIBUTES wlanSecurityAttributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WLAN_ASSOCIATION_ATTRIBUTES
    {
        public DOT11_SSID dot11Ssid;
        public int dot11BssType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] dot11Bssid;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private byte[] padding;

        public int dot11PhyType;
        public uint uDot11PhyIndex;
        public uint wlanSignalQuality;
        public uint ulRxRate;
        public uint ulTxRate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DOT11_SSID
    {
        public uint uSSIDLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ucSSID;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WLAN_SECURITY_ATTRIBUTES
    {
        public int bSecurityEnabled;
        public int bOneXEnabled;
        public int dot11AuthAlgorithm;
        public int dot11CipherAlgorithm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WLAN_BSS_LIST
    {
        public uint dwTotalSize;
        public uint dwNumberOfItems;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WLAN_BSS_ENTRY
    {
        public DOT11_SSID dot11Ssid;
        public uint uPhyId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] dot11Bssid;

        public DOT11_BSS_TYPE dot11BssType;
        public uint dot11BssPhyType;
        public int lRssi;
        public uint uLinkQuality;
        public int bInRegDomain;
        public ushort usBeaconPeriod;
        public ulong ullTimestamp;
        public ulong ullHostTimestamp;
        public ushort usCapabilityInformation;
        public uint ulChCenterFrequency;
        public WLAN_RATE_SET wlanRateSet;
        public uint ulIeOffset;
        public uint ulIeSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WLAN_RATE_SET
    {
        public uint uRateSetLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
        public ushort[] usRateSet;
    }
}
