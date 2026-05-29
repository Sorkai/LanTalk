using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanTalk.App.Services;

public static class NetworkInterfaceHelper
{
    public static string GetLocalIpAddress()
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
            .Select(address => address.Address.ToString())
            .FirstOrDefault();

        return address ?? "127.0.0.1";
    }
}

