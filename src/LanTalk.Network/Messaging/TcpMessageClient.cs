using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LanTalk.Core.Models;
using LanTalk.Core.Serialization;

namespace LanTalk.Network.Messaging;

public sealed class TcpMessageClient
{
    public async Task SendAsync(string ipAddress, int port, NetworkPacket packet, CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);

        await using var stream = client.GetStream();
        var json = JsonSerializer.Serialize(packet, LanTalkJsonContext.Default.NetworkPacket);
        var payload = Encoding.UTF8.GetBytes(json);
        var length = BitConverter.GetBytes(payload.Length);

        await stream.WriteAsync(length, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }
}

