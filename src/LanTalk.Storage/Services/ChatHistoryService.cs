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

    public Task<IReadOnlyList<ChatMessage>> SearchMessagesAsync(string sessionId, string query, CancellationToken cancellationToken = default)
    {
        return _messageRepository.SearchMessagesAsync(sessionId, query, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<ChatMessage>> MarkSessionIncomingMessagesReadAsync(
        string sessionId,
        DateTimeOffset readTime,
        CancellationToken cancellationToken = default)
    {
        return _messageRepository.MarkSessionIncomingMessagesReadAsync(sessionId, readTime, cancellationToken);
    }

    public Task<(int ReadByCount, int ReadTargetCount, bool IsRead)> MarkMessageReadByAsync(
        MessageReadReceiptPayload receipt,
        CancellationToken cancellationToken = default)
    {
        return _messageRepository.MarkMessageReadByAsync(receipt, cancellationToken);
    }

    public Task RecallMessageAsync(
        string sessionId,
        string messageId,
        DateTimeOffset recalledTime,
        CancellationToken cancellationToken = default)
    {
        return _messageRepository.RecallMessageAsync(sessionId, messageId, recalledTime, cancellationToken);
    }
}
