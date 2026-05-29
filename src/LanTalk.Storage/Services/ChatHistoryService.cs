using LanTalk.Core.Models;
using LanTalk.Storage.Repositories;

namespace LanTalk.Storage.Services;

public sealed class ChatHistoryService
{
    private readonly MessageRepository _messageRepository;

    public ChatHistoryService(MessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        return _messageRepository.SaveAsync(message, cancellationToken);
    }

    public Task<IReadOnlyList<ChatMessage>> LoadRecentMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return _messageRepository.LoadRecentMessagesAsync(sessionId, cancellationToken: cancellationToken);
    }
}

