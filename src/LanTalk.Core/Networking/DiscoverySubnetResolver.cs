using System.Net;
using LanTalk.Core.Constants;

namespace LanTalk.Core.Networking;

public static class DiscoverySubnetResolver
{
    private static readonly char[] Separators = [',', ';', '，', '；'];

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = NetworkConstants.DefaultDiscoverySubnet;

        var tokens = SplitTokens(value);
        if (tokens.Length == 0)
        {
            return true;
        }

        var normalizedTargets = new List<string>();
        foreach (var token in tokens)
        {
            if (IsAuto(token))
            {
                normalizedTargets.Add(NetworkConstants.DefaultDiscoverySubnet);
                continue;
            }

            if (TryParseWildcardSubnet(token, out var wildcardNormalized, out _))
            {
                normalizedTargets.Add(wildcardNormalized);
                continue;
            }

            if (TryParseCidrSubnet(token, out var cidrNormalized, out _))
            {
                normalizedTargets.Add(cidrNormalized);
                continue;
            }

            if (TryParseIpv4(token, out var address))
            {
                normalizedTargets.Add(address.ToString());
                continue;
            }

            return false;
        }

        normalized = string.Join(", ", normalizedTargets.Distinct(StringComparer.OrdinalIgnoreCase));
        return true;
    }

    public static IReadOnlyList<IPAddress> GetBroadcastAddresses(string? value)
    {
        if (!TryGetBroadcastAddresses(value, out var addresses))
        {
            return [IPAddress.Broadcast];
        }

        return addresses;
    }

    public static bool TryGetBroadcastAddresses(string? value, out IReadOnlyList<IPAddress> addresses)
    {
        addresses = [IPAddress.Broadcast];

        var tokens = SplitTokens(value);
        if (tokens.Length == 0)
        {
            return true;
        }

        var targets = new List<IPAddress>();
        foreach (var token in tokens)
        {
            if (IsAuto(token))
            {
                targets.Add(IPAddress.Broadcast);
                continue;
            }

            if (TryParseWildcardSubnet(token, out _, out var wildcardBroadcast))
            {
                targets.Add(wildcardBroadcast);
                continue;
            }

            if (TryParseCidrSubnet(token, out _, out var cidrBroadcast))
            {
                targets.Add(cidrBroadcast);
                continue;
            }

            if (TryParseIpv4(token, out var address))
            {
                targets.Add(address);
                continue;
            }

            return false;
        }

        addresses = targets
            .DistinctBy(address => address.ToString())
            .ToArray();
        return true;
    }

    private static string[] SplitTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool IsAuto(string value)
    {
        return string.Equals(value, NetworkConstants.DefaultDiscoverySubnet, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "自动", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseWildcardSubnet(string value, out string normalized, out IPAddress broadcastAddress)
    {
        normalized = string.Empty;
        broadcastAddress = IPAddress.Broadcast;

        var parts = value.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || parts[3] != "*")
        {
            return false;
        }

        var addressText = $"{parts[0]}.{parts[1]}.{parts[2]}.0";
        if (!TryParseIpv4(addressText, out var address))
        {
            return false;
        }

        return TryNormalizeCidr(address, 24, out normalized, out broadcastAddress);
    }

    private static bool TryParseCidrSubnet(string value, out string normalized, out IPAddress broadcastAddress)
    {
        normalized = string.Empty;
        broadcastAddress = IPAddress.Broadcast;

        var slashIndex = value.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == value.Length - 1)
        {
            return false;
        }

        var addressText = value[..slashIndex].Trim();
        var prefixText = value[(slashIndex + 1)..].Trim();
        if (!TryParseIpv4(addressText, out var address) ||
            !int.TryParse(prefixText, out var prefixLength) ||
            prefixLength is < 0 or > 32)
        {
            return false;
        }

        return TryNormalizeCidr(address, prefixLength, out normalized, out broadcastAddress);
    }

    private static bool TryNormalizeCidr(IPAddress address, int prefixLength, out string normalized, out IPAddress broadcastAddress)
    {
        var addressValue = ToUInt32(address);
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        var network = addressValue & mask;
        var broadcast = network | ~mask;

        var networkAddress = FromUInt32(network);
        broadcastAddress = FromUInt32(broadcast);
        normalized = $"{networkAddress}/{prefixLength}";
        return true;
    }

    private static bool TryParseIpv4(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out address!) &&
            address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return true;
        }

        address = IPAddress.None;
        return false;
    }

    private static uint ToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
    }

    private static IPAddress FromUInt32(uint value)
    {
        return new IPAddress(
        [
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        ]);
    }
}
