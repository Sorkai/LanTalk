using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;
using LanTalk.Core.Models;
using LanTalk.Core.Networking;
using LanTalk.Core.Serialization;
using LanTalk.Core.Services;
using LanTalk.App.Services;
using LanTalk.Network.Discovery;
using LanTalk.Network.Files;
using LanTalk.Network.Messaging;
using LanTalk.Storage.Database;
using LanTalk.Storage.Repositories;
using LanTalk.Storage.Services;
using LanTalk.Storage.Settings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace LanTalk.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly ChatHistoryService _chatHistoryService;
    private readonly DiscoveryService _discoveryService;
    private readonly MessageService _messageService;
    private readonly FileTransferService _fileTransferService;
    private readonly FileTransferRepository _fileTransferRepository;
    private readonly UserRepository _userRepository;
    private readonly GroupRepository _groupRepository;
    private readonly OutgoingDeliveryRepository _outgoingDeliveryRepository;
    private readonly TcpFileServer _fileServer;
    private readonly ILanTalkLogger _logger;
    private readonly Dictionary<string, FileTransferRequest> _pendingFileRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _outgoingFilePaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UserInfo> _outgoingFileReceivers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatMessageViewModel> _outgoingFileMessages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GroupFileTransferState> _outgoingGroupFileStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OutgoingBatchTransfer> _outgoingBatchTransfers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileBatchTransferState> _outgoingBatchStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileBatchTransferState> _incomingBatchStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatMessageViewModel> _incomingFileMessages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _incomingSavePaths = new(StringComparer.Ordinal);
    private readonly HashSet<string> _incomingFinishedNotifications = new(StringComparer.Ordinal);
    private readonly HashSet<string> _retryingDeliveryRecipients = new(StringComparer.Ordinal);
    private readonly HashSet<string> _encryptedGroupSessions = new(StringComparer.Ordinal);
    private int _previewImagePixelWidth;
    private int _previewImagePixelHeight;
    private CancellationTokenSource? _messageSearchCts;
    private bool _isUpdatingEncryptionToggle;
    private CancellationTokenSource? _fileServerCts;
    private Task? _fileServerTask;
    private AppSettings _settings = new();
    private OnlineUserViewModel? _broadcastSession;

    [ObservableProperty]
    private string localNickname = "LanTalk 用户";

    public string LocalAvatarText => AvatarService.GetInitial(LocalNickname);

    public IBrush LocalAvatarBrush => AvatarService.CreateBrush($"{_settings.UserId}:{LocalNickname}");

    [ObservableProperty]
    private string localDepartment = NetworkConstants.DefaultDepartment;

    [ObservableProperty]
    private string localIpAddress = "正在检测";

    [ObservableProperty]
    private string localStatusText = "在线";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string messageSearchText = string.Empty;

    [ObservableProperty]
    private string messageSearchStatus = string.Empty;

    [ObservableProperty]
    private OnlineUserViewModel? selectedUser;

    [ObservableProperty]
    private string currentSessionTitle = "请选择一个会话";

    [ObservableProperty]
    private string currentSessionSubtitle = "启动后会自动发现同一局域网内的在线用户";

    [ObservableProperty]
    private bool canToggleConversationEncryption;

    [ObservableProperty]
    private bool isConversationEncryptionEnabled;

    [ObservableProperty]
    private string conversationEncryptionStatus = "端到端加密未启用";

    [ObservableProperty]
    private string conversationEncryptionFingerprint = "选择私聊会话后可启用端到端加密。";

    [ObservableProperty]
    private string draftMessage = string.Empty;

    [ObservableProperty]
    private bool isSettingsPaneOpen;

    [ObservableProperty]
    private SettingsViewModel settings = new();

    [ObservableProperty]
    private string statusMessage = "正在初始化 LanTalk";

    [ObservableProperty]
    private int totalUnreadCount;

    [ObservableProperty]
    private string unreadSummary = "没有未读消息";

    [ObservableProperty]
    private bool isFileRequestPaneOpen;

    [ObservableProperty]
    private FileReceiveRequestViewModel? pendingFileRequest;

    [ObservableProperty]
    private bool isGroupPaneOpen;

    [ObservableProperty]
    private string groupDraftName = string.Empty;

    [ObservableProperty]
    private bool isPermanentGroupDraft = true;

    [ObservableProperty]
    private string groupDraftStatus = string.Empty;

    [ObservableProperty]
    private bool canDeleteSelectedGroup;

    [ObservableProperty]
    private bool isEmojiPickerOpen;

    [ObservableProperty]
    private bool isImagePreviewOpen;

    [ObservableProperty]
    private Bitmap? previewImageSource;

    [ObservableProperty]
    private string previewImageTitle = string.Empty;

    [ObservableProperty]
    private double previewImageZoom = 1;

    [ObservableProperty]
    private string previewImageZoomText = "100%";

    [ObservableProperty]
    private double previewImageDisplayWidth;

    [ObservableProperty]
    private double previewImageDisplayHeight;

    public ObservableCollection<OnlineUserViewModel> OnlineUsers { get; } = [];

    public ObservableCollection<OnlineUserViewModel> RecentSessions { get; } = [];

    public ObservableCollection<OnlineUserViewModel> FilteredOnlineUsers { get; } = [];

    public ObservableCollection<OnlineUserViewModel> FilteredRecentSessions { get; } = [];

    public ObservableCollection<ContactGroupViewModel> ContactGroups { get; } = [];

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    public ObservableCollection<GroupMemberCandidateViewModel> GroupMemberCandidates { get; } = [];

    public IReadOnlyList<string> CommonEmojis { get; } =
    [
        "😀", "😂", "😊", "😍", "👍", "👏", "🙏", "💪",
        "🎉", "🔥", "✅", "⭐", "💡", "📌", "☕", "❤️",
        "😄", "😅", "😉", "😎", "😭", "🤔", "👌", "🚀"
    ];

    public int OnlineCount => OnlineUsers.Count(user => user.Status == UserStatus.Online);

    public string WindowTitle => TotalUnreadCount > 0 ? $"LanTalk ({TotalUnreadCount})" : "LanTalk";

    public event EventHandler<UserNotificationEventArgs>? UserNotificationRequested;

    public MainWindowViewModel()
        : this(CreateDefaultServices())
    {
    }

    public MainWindowViewModel(AppServices services)
    {
        _settingsService = services.SettingsService;
        _chatHistoryService = services.ChatHistoryService;
        _discoveryService = services.DiscoveryService;
        _messageService = services.MessageService;
        _fileTransferService = services.FileTransferService;
        _fileTransferRepository = services.FileTransferRepository;
        _userRepository = services.UserRepository;
        _groupRepository = services.GroupRepository;
        _outgoingDeliveryRepository = services.OutgoingDeliveryRepository;
        _fileServer = services.FileServer;
        _logger = services.Logger;
        _discoveryService.UsersChanged += OnDiscoveryUsersChanged;
        _messageService.PacketReceived += OnMessagePacketReceived;
        _messageService.EncryptionStateChanged += OnEncryptionStateChanged;
        _messageService.EncryptionError += OnEncryptionError;

        EnsureBroadcastSession();
        RefreshFilteredUsers();
        _ = InitializeAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredUsers();
    }

    partial void OnLocalNicknameChanged(string value)
    {
        OnPropertyChanged(nameof(LocalAvatarText));
        OnPropertyChanged(nameof(LocalAvatarBrush));
    }

    partial void OnMessageSearchTextChanged(string value)
    {
        _ = ApplyMessageSearchAsync(value);
    }

    partial void OnTotalUnreadCountChanged(int value)
    {
        UnreadSummary = value == 0 ? "没有未读消息" : $"{value} 条未读消息";
        OnPropertyChanged(nameof(WindowTitle));
    }

    partial void OnPreviewImageZoomChanged(double value)
    {
        PreviewImageZoomText = $"{value * 100:0}%";
        UpdatePreviewImageSize();
    }

    partial void OnSelectedUserChanged(OnlineUserViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        RefreshConversationEncryptionState();
        UpdateCurrentSessionHeader();
        CanDeleteSelectedGroup = value.IsGroupSession;
        value.UnreadCount = 0;
        RefreshSelectionState(value);
        RefreshUnreadState();
        UpsertRecentSession(value, moveToTop: true);
        _ = LoadSessionMessagesAsync(value);
        _ = MarkSessionMessagesReadAsync(value);
    }

    partial void OnIsConversationEncryptionEnabledChanged(bool value)
    {
        if (_isUpdatingEncryptionToggle)
        {
            return;
        }

        _ = SetConversationEncryptionAsync(value);
    }

    private async Task SetConversationEncryptionAsync(bool isEnabled)
    {
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            RefreshConversationEncryptionState();
            return;
        }

        if (SelectedUser.IsGroupSession)
        {
            await SetGroupConversationEncryptionAsync(SelectedUser, isEnabled);
            return;
        }

        var receiver = ToUserInfo(SelectedUser);

        try
        {
            if (isEnabled)
            {
                await _messageService.EnableEncryptionAsync(_settings, receiver);
                StatusMessage = $"正在与 {SelectedUser.Nickname} 协商端到端加密。";
            }
            else
            {
                await _messageService.DisableEncryptionAsync(_settings, receiver);
                StatusMessage = $"已关闭与 {SelectedUser.Nickname} 的端到端加密。";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(isEnabled ? "启用端到端加密失败。" : "关闭端到端加密失败。", ex);
            StatusMessage = isEnabled
                ? $"启用端到端加密失败：{ex.Message}"
                : $"关闭端到端加密失败：{ex.Message}";
        }
        finally
        {
            RefreshConversationEncryptionState();
            UpdateCurrentSessionHeader();
        }
    }

    private async Task SetGroupConversationEncryptionAsync(OnlineUserViewModel group, bool isEnabled)
    {
        try
        {
            if (isEnabled)
            {
                _encryptedGroupSessions.Add(group.UserId);
                var started = await EnsureGroupMemberEncryptionAsync(group);
                StatusMessage = started == 0
                    ? "已启用群组端到端加密，群消息将按成员逐一加密发送。"
                    : $"已启用群组端到端加密，并向 {started} 个成员发起一对一加密协商。";
            }
            else
            {
                _encryptedGroupSessions.Remove(group.UserId);
                StatusMessage = "已关闭该群组的端到端加密，后续群消息将按普通群组消息发送。";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(isEnabled ? "启用群组端到端加密失败。" : "关闭群组端到端加密失败。", ex);
            StatusMessage = isEnabled
                ? $"启用群组端到端加密失败：{ex.Message}"
                : $"关闭群组端到端加密失败：{ex.Message}";
        }
        finally
        {
            RefreshConversationEncryptionState();
            UpdateCurrentSessionHeader();
        }
    }

    private async Task<int> EnsureGroupMemberEncryptionAsync(OnlineUserViewModel group)
    {
        var started = 0;
        foreach (var receiver in ResolveGroupReceivers(group).Where(user => user.Status == UserStatus.Online))
        {
            var state = _messageService.GetEncryptionState(receiver.UserId);
            if (state.IsEnabled || state.IsPending)
            {
                continue;
            }

            try
            {
                await _messageService.EnableEncryptionAsync(_settings, receiver);
                started++;
            }
            catch (Exception ex)
            {
                _logger.Warning($"向群组成员 {receiver.Nickname} 发起端到端加密协商失败：{ex.Message}");
            }
        }

        return started;
    }

    private void OnEncryptionStateChanged(object? sender, ConversationEncryptionStateChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SelectedUser?.UserId == e.State.UserId ||
                (SelectedUser?.IsGroupSession == true && SelectedUser.GroupMemberIds.Contains(e.State.UserId, StringComparer.Ordinal)))
            {
                RefreshConversationEncryptionState();
                UpdateCurrentSessionHeader();
            }

            StatusMessage = e.State.StatusText;

            if (e.State.IsEnabled)
            {
                var user = OnlineUsers.FirstOrDefault(item => item.UserId == e.State.UserId && item.Status == UserStatus.Online);
                if (user is not null)
                {
                    _ = RetryPendingDeliveriesAsync(user);
                }
            }
        });
    }

    private void OnEncryptionError(object? sender, EncryptionErrorEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SelectedUser?.UserId == e.PeerUserId ||
                (SelectedUser?.IsGroupSession == true && SelectedUser.GroupMemberIds.Contains(e.PeerUserId, StringComparer.Ordinal)))
            {
                RefreshConversationEncryptionState();
                UpdateCurrentSessionHeader();
            }

            StatusMessage = e.Message;
        });
    }

    private void RefreshConversationEncryptionState()
    {
        if (SelectedUser is null)
        {
            ApplyConversationEncryptionState(false, false, "端到端加密未启用", "选择私聊会话后可启用端到端加密。");
            return;
        }

        if (SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            ApplyConversationEncryptionState(false, false, "广播不支持端到端加密", "端到端加密只用于一对一私聊文本。");
            return;
        }

        if (SelectedUser.IsGroupSession)
        {
            var isEnabled = _encryptedGroupSessions.Contains(SelectedUser.UserId);
            ApplyConversationEncryptionState(
                true,
                isEnabled,
                isEnabled ? "群组端到端加密已启用 · 逐成员加密" : "群组端到端加密未启用",
                DescribeGroupEncryptionReadiness(SelectedUser, isEnabled));
            return;
        }

        var state = _messageService.GetEncryptionState(SelectedUser.UserId);
        var tip = string.IsNullOrWhiteSpace(state.Fingerprint)
            ? "开启后会使用临时 ECDH 密钥协商与 AES-GCM 加密私聊文本。"
            : $"请与对方比对加密指纹：{state.Fingerprint}";
        ApplyConversationEncryptionState(true, state.IsEnabled || state.IsPending, state.StatusText, tip);
    }

    private string DescribeGroupEncryptionReadiness(OnlineUserViewModel group, bool isEnabled)
    {
        var receivers = ResolveGroupReceivers(group).ToArray();
        if (receivers.Length == 0)
        {
            return "群组没有其他成员，暂时无需加密发送。";
        }

        var onlineReceivers = receivers.Where(user => user.Status == UserStatus.Online).ToArray();
        var readyCount = onlineReceivers.Count(user => _messageService.GetEncryptionState(user.UserId).IsEnabled);
        var pendingCount = onlineReceivers.Count(user => _messageService.GetEncryptionState(user.UserId).IsPending);
        var offlineCount = receivers.Length - onlineReceivers.Length;

        if (!isEnabled)
        {
            return "开启后会复用一对一端到端会话逐成员加密群文本；未协商或离线成员会进入加密补发队列。";
        }

        return $"在线成员 {onlineReceivers.Length} 人，已加密 {readyCount} 人，协商中 {pendingCount} 人，离线 {offlineCount} 人。图片和文件仍使用当前附件传输协议。";
    }

    private void ApplyConversationEncryptionState(bool canToggle, bool isChecked, string status, string fingerprintTip)
    {
        _isUpdatingEncryptionToggle = true;
        try
        {
            CanToggleConversationEncryption = canToggle;
            IsConversationEncryptionEnabled = isChecked;
            ConversationEncryptionStatus = status;
            ConversationEncryptionFingerprint = fingerprintTip;
        }
        finally
        {
            _isUpdatingEncryptionToggle = false;
        }
    }

    private void UpdateCurrentSessionHeader()
    {
        if (SelectedUser is null)
        {
            CurrentSessionTitle = "请选择一个会话";
            CurrentSessionSubtitle = "启动后会自动发现同一局域网内的在线用户";
            return;
        }

        CurrentSessionTitle = SelectedUser.Nickname;
        if (SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            CurrentSessionSubtitle = "广播给当前所有在线用户";
            return;
        }

        CurrentSessionSubtitle = SelectedUser.IsGroupSession
            ? $"{SelectedUser.GroupKindText} · {SelectedUser.GroupMemberCount} 人 · 点对点多人会话"
            : $"{SelectedUser.DepartmentText} · {SelectedUser.IpAddress} · {SelectedUser.StatusText} · {ConversationEncryptionStatus}";
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var content = DraftMessage.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (SelectedUser is null)
        {
            StatusMessage = "请先选择一个在线用户或广播会话。";
            return;
        }

        var kind = SelectedUser.IsGroupSession
            ? MessageKind.Group
            : SelectedUser.UserId == NetworkConstants.BroadcastSessionId
            ? MessageKind.Broadcast
            : MessageKind.Private;

        var sessionId = SelectedUser.UserId == NetworkConstants.BroadcastSessionId
            ? NetworkConstants.BroadcastSessionId
            : SelectedUser.UserId;

        var message = new ChatMessage
        {
            SessionId = sessionId,
            SenderId = _settings.UserId,
            ReceiverId = SelectedUser.UserId,
            Kind = kind,
            Content = content,
            SendTime = DateTimeOffset.Now,
            IsMine = true,
            ReadTargetCount = GetReadTargetCount(SelectedUser, kind)
        };

        DraftMessage = string.Empty;
        UpsertRecentSession(SelectedUser, content, moveToTop: true);

        if (_settings.SaveChatHistory)
        {
            await _chatHistoryService.SaveMessageAsync(message);
        }

        if (IsMessageSearchActive())
        {
            await SearchSessionMessagesAsync(SelectedUser, MessageSearchText);
        }
        else
        {
            Messages.Add(ToMessageViewModel(message, LocalNickname));
        }

        try
        {
            if (kind == MessageKind.Group)
            {
                var payload = new GroupMessagePayload(
                    message.MessageId,
                    SelectedUser.UserId,
                    SelectedUser.Nickname,
                    SelectedUser.GroupKind,
                    SelectedUser.GroupMemberIds.ToArray(),
                    LocalNickname,
                    content);
                var result = await SendGroupMessageWithOfflineQueueAsync(SelectedUser, payload);
                var groupPrefix = result.IsEncrypted ? "群组加密消息" : "群组消息";
                StatusMessage = result.QueuedCount == 0 && result.FailureCount == 0
                    ? $"{groupPrefix}已发送给 {result.SuccessCount} 个在线成员。"
                    : $"{groupPrefix}已发送 {result.SuccessCount} 人，待补发 {result.QueuedCount} 人，失败 {result.FailureCount} 人。";
            }
            else if (kind == MessageKind.Broadcast)
            {
                var result = await _messageService.BroadcastAsync(_settings, OnlineUsers.Select(ToUserInfo), content);
                StatusMessage = result.FailureCount == 0
                    ? $"广播消息已发送给 {result.SuccessCount} 个在线用户。"
                    : $"广播已发送 {result.SuccessCount} 人，失败 {result.FailureCount} 人。";
            }
            else
            {
                await _messageService.SendPrivateMessageAsync(_settings, ToUserInfo(SelectedUser), message);
                StatusMessage = "私聊消息已发送。";
            }
        }
        catch (Exception ex)
        {
            _logger.Error("消息发送失败。", ex);
            StatusMessage = $"消息已保存，但发送失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        Settings = SettingsViewModel.FromSettings(_settings);
        IsSettingsPaneOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsPaneOpen = false;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var validationError = ValidatePorts(Settings);
        if (validationError is not null)
        {
            StatusMessage = validationError;
            return;
        }

        var discoverySubnetText = Settings.GetDiscoverySubnetText();
        if (!DiscoverySubnetResolver.TryNormalize(discoverySubnetText, out var normalizedDiscoverySubnet))
        {
            StatusMessage = "自动发现网段格式不正确，请逐行使用 Auto、192.168.1.0/24、192.168.1.* 或 192.168.1.255。";
            return;
        }

        var portsChanged =
            _settings.UdpPort != Settings.UdpPort ||
            _settings.MessagePort != Settings.MessagePort ||
            _settings.FilePort != Settings.FilePort ||
            !string.Equals(_settings.DiscoverySubnet, normalizedDiscoverySubnet, StringComparison.OrdinalIgnoreCase);

        _settings.Nickname = Settings.Nickname.Trim();
        _settings.Department = NormalizeDepartment(Settings.Department);
        _settings.FileSavePath = Settings.FileSavePath.Trim();
        _settings.SaveChatHistory = Settings.SaveChatHistory;
        _settings.ThemeMode = Settings.ThemeMode;
        _settings.ThemeColor = Settings.ThemeColor;
        _settings.UdpPort = Settings.UdpPort;
        _settings.MessagePort = Settings.MessagePort;
        _settings.FilePort = Settings.FilePort;
        _settings.DiscoverySubnet = normalizedDiscoverySubnet;

        await _settingsService.SaveAsync(_settings);
        AppThemeService.Apply(_settings);
        LocalNickname = _settings.Nickname;
        LocalDepartment = _settings.Department;
        IsSettingsPaneOpen = false;
        StatusMessage = portsChanged
            ? "设置已保存。网络发现设置变更将在重启 LanTalk 后生效。"
            : "设置已保存。";
    }

    [RelayCommand]
    private async Task AcceptFileRequestAsync()
    {
        if (PendingFileRequest is null || !_pendingFileRequests.TryGetValue(PendingFileRequest.FileId, out var request))
        {
            IsFileRequestPaneOpen = false;
            return;
        }

        var sender = OnlineUsers.FirstOrDefault(user => user.UserId == request.SenderId);
        if (sender is null)
        {
            StatusMessage = "发送方不在线，无法接收文件。";
            IsFileRequestPaneOpen = false;
            return;
        }

        var savePath = _incomingSavePaths[request.FileId];
        var fileMessage = _incomingFileMessages[request.FileId];
        fileMessage.StatusText = "已接受，等待传输";

        if (request.IsBatchTransfer)
        {
            await AcceptBatchFileRequestAsync(request, sender, fileMessage);
            IsFileRequestPaneOpen = false;
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? _settings.FileSavePath);
        var resumeOffset = GetResumeOffset(savePath, request.FileSize);
        fileMessage.StatusText = resumeOffset > 0 ? "已接受，等待断点续传" : "已接受，等待传输";

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SavePath = savePath,
            TransferKind = request.TransferKind,
            BatchId = request.BatchId,
            RelativePath = request.RelativePath,
            BytesTransferred = resumeOffset,
            Status = FileTransferStatus.Accepted,
            TransferTime = DateTimeOffset.Now
        });

        await _messageService.SendFileResponseAsync(_settings, ToUserInfo(sender), new FileTransferResponse(request.FileId, true, ResumeOffset: resumeOffset));
        StatusMessage = $"已接受文件：{request.FileName}";
        IsFileRequestPaneOpen = false;
    }

    [RelayCommand]
    private async Task RejectFileRequestAsync()
    {
        if (PendingFileRequest is null || !_pendingFileRequests.TryGetValue(PendingFileRequest.FileId, out var request))
        {
            IsFileRequestPaneOpen = false;
            return;
        }

        var sender = OnlineUsers.FirstOrDefault(user => user.UserId == request.SenderId);
        if (sender is not null)
        {
            await _messageService.SendFileResponseAsync(_settings, ToUserInfo(sender), new FileTransferResponse(request.FileId, false, "接收方拒绝"));
        }

        if (_incomingFileMessages.TryGetValue(request.FileId, out var fileMessage))
        {
            fileMessage.StatusText = "已拒绝";
        }

        if (request.IsBatchTransfer)
        {
            await RejectBatchFileRequestAsync(request);
            StatusMessage = $"已拒绝{GetTransferKindText(request.TransferKind)}：{request.FileName}";
            IsFileRequestPaneOpen = false;
            return;
        }

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SavePath = _incomingSavePaths.GetValueOrDefault(request.FileId),
            Status = FileTransferStatus.Rejected,
            TransferTime = DateTimeOffset.Now
        });

        StatusMessage = $"已拒绝文件：{request.FileName}";
        IsFileRequestPaneOpen = false;
    }

    [RelayCommand]
    private void SelectUser(OnlineUserViewModel? user)
    {
        if (user is not null)
        {
            SelectedUser = user;
        }
    }

    [RelayCommand]
    private void SelectBroadcast()
    {
        SelectedUser = _broadcastSession;
    }

    [RelayCommand]
    private void OpenGroupPane()
    {
        GroupDraftName = $"群组 {DateTimeOffset.Now:HHmm}";
        IsPermanentGroupDraft = true;
        GroupDraftStatus = string.Empty;
        ReplaceCollection(
            GroupMemberCandidates,
            OnlineUsers
                .Where(user => !user.IsGroupSession && user.UserId != _settings.UserId)
                .OrderByDescending(user => user.Status == UserStatus.Online)
                .ThenBy(user => user.DepartmentText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(user => user.Nickname, StringComparer.OrdinalIgnoreCase)
                .Select(user => new GroupMemberCandidateViewModel
                {
                    UserId = user.UserId,
                    Nickname = user.Nickname,
                    Department = user.DepartmentText,
                    Status = user.Status,
                    IsSelected = user.Status == UserStatus.Online
                }));
        IsGroupPaneOpen = true;
    }

    [RelayCommand]
    private void CloseGroupPane()
    {
        IsGroupPaneOpen = false;
        GroupDraftStatus = string.Empty;
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        var selectedMembers = GroupMemberCandidates
            .Where(candidate => candidate.IsSelected)
            .Select(candidate => candidate.UserId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (selectedMembers.Count == 0)
        {
            GroupDraftStatus = "请至少选择一名联系人。";
            return;
        }

        var name = GroupDraftName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            GroupDraftStatus = "请输入群组名称。";
            return;
        }

        selectedMembers.Add(_settings.UserId);
        var group = new ChatGroup
        {
            GroupId = Guid.NewGuid().ToString("N"),
            Name = name,
            Kind = IsPermanentGroupDraft ? GroupKind.Permanent : GroupKind.Temporary,
            MemberUserIds = selectedMembers.Distinct(StringComparer.Ordinal).ToArray(),
            CreatedTime = DateTimeOffset.Now,
            UpdatedTime = DateTimeOffset.Now
        };

        var session = EnsureGroupSession(group);
        if (group.Kind == GroupKind.Permanent)
        {
            await _groupRepository.SaveAsync(group);
        }

        UpsertRecentSession(session, $"{session.GroupKindText} · {session.GroupMemberCount} 人", moveToTop: true);
        SelectedUser = session;
        IsGroupPaneOpen = false;
        StatusMessage = $"{session.GroupKindText}“{session.Nickname}”已创建。";
        await LoadSessionMessagesAsync(session);
    }

    [RelayCommand]
    private async Task DeleteSelectedGroupAsync()
    {
        if (SelectedUser is null || !SelectedUser.IsGroupSession)
        {
            return;
        }

        var group = SelectedUser;
        RecentSessions.Remove(group);
        if (group.GroupKind == GroupKind.Permanent)
        {
            await _groupRepository.DeleteAsync(group.UserId);
        }

        SelectedUser = RecentSessions.FirstOrDefault();
        CanDeleteSelectedGroup = SelectedUser?.IsGroupSession == true;
        StatusMessage = $"已移除群组：{group.Nickname}";
        RefreshFilteredUsers();
    }

    [RelayCommand]
    private void ToggleEmojiPicker()
    {
        IsEmojiPickerOpen = !IsEmojiPickerOpen;
    }

    [RelayCommand]
    private void InsertEmoji(string? emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
        {
            return;
        }

        DraftMessage += emoji;
    }

    [RelayCommand]
    private void OpenImagePreview(ChatMessageViewModel? message)
    {
        if (message?.ImageSource is null)
        {
            StatusMessage = "图片文件不可用，无法预览。";
            return;
        }

        PreviewImageSource = message.ImageSource;
        PreviewImageTitle = string.IsNullOrWhiteSpace(message.FileName) ? "图片预览" : message.FileName;
        _previewImagePixelWidth = message.ImagePixelWidth > 0 ? message.ImagePixelWidth : 800;
        _previewImagePixelHeight = message.ImagePixelHeight > 0 ? message.ImagePixelHeight : 600;
        PreviewImageZoom = 1;
        UpdatePreviewImageSize();
        IsImagePreviewOpen = true;
    }

    [RelayCommand]
    private void CloseImagePreview()
    {
        IsImagePreviewOpen = false;
        PreviewImageSource = null;
        PreviewImageTitle = string.Empty;
        _previewImagePixelWidth = 0;
        _previewImagePixelHeight = 0;
        PreviewImageDisplayWidth = 0;
        PreviewImageDisplayHeight = 0;
    }

    [RelayCommand]
    private void ZoomInImagePreview()
    {
        PreviewImageZoom = Math.Min(4, PreviewImageZoom + 0.25);
    }

    [RelayCommand]
    private void ZoomOutImagePreview()
    {
        PreviewImageZoom = Math.Max(0.25, PreviewImageZoom - 0.25);
    }

    [RelayCommand]
    private void ResetImagePreviewZoom()
    {
        PreviewImageZoom = 1;
    }

    [RelayCommand]
    private void SelectNextRecentSession()
    {
        SelectRelativeRecentSession(1);
    }

    [RelayCommand]
    private void SelectPreviousRecentSession()
    {
        SelectRelativeRecentSession(-1);
    }

    [RelayCommand]
    private void SelectNextUnreadSession()
    {
        var unreadSessions = OrderRecentSessions(RecentSessions.Where(session => session.UnreadCount > 0)).ToArray();
        if (unreadSessions.Length == 0)
        {
            StatusMessage = "没有未读会话。";
            return;
        }

        var currentIndex = FindSessionIndex(unreadSessions, SelectedUser?.UserId);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % unreadSessions.Length;
        SelectedUser = unreadSessions[nextIndex];
    }

    [RelayCommand]
    private async Task RecallMessageAsync(ChatMessageViewModel? message)
    {
        if (message is null || !message.CanRecall)
        {
            StatusMessage = "只能撤回自己发送的文本或图片消息。";
            return;
        }

        var session = RecentSessions.FirstOrDefault(item => item.UserId == message.SessionId)
            ?? SelectedUser;
        if (session is null)
        {
            StatusMessage = "未找到消息所在会话，无法撤回。";
            return;
        }

        var recallTime = DateTimeOffset.Now;
        await _chatHistoryService.RecallMessageAsync(message.SessionId, message.MessageId, recallTime);
        MarkMessageRecalled(message, isMine: true);

        var payload = new MessageRecallPayload(
            message.MessageId,
            message.SessionId,
            _settings.UserId,
            LocalNickname,
            session.IsGroupSession,
            recallTime);

        if (session.IsGroupSession)
        {
            foreach (var memberId in session.GroupMemberIds.Where(id => id != _settings.UserId).Distinct(StringComparer.Ordinal))
            {
                await SendRecallToRecipientAsync(memberId, payload);
            }

            StatusMessage = $"已撤回群组“{session.Nickname}”中的一条消息。";
            UpsertRecentSession(session, "你撤回了一条消息", moveToTop: true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.ReceiverId))
        {
            await SendRecallToRecipientAsync(message.ReceiverId, payload);
        }

        StatusMessage = "已撤回一条消息。";
        UpsertRecentSession(session, "你撤回了一条消息", moveToTop: true);
    }

    [RelayCommand]
    private async Task RefreshDiscoveryAsync()
    {
        try
        {
            await _discoveryService.RefreshAsync();
            StatusMessage = "已重新广播上线消息，正在刷新局域网在线用户。";
        }
        catch (Exception ex)
        {
            _logger.Error("刷新局域网用户失败。", ex);
            StatusMessage = $"刷新局域网用户失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AttachFileAsync()
    {
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            StatusMessage = "请先选择一个用户或群组再发送文件。";
            return;
        }

        var selectedPaths = await PickFilesAsync();
        if (selectedPaths.Count == 0)
        {
            return;
        }

        if (selectedPaths.Count == 1)
        {
            await SendSelectedFileAsync(selectedPaths[0], isImage: false);
            return;
        }

        await SendSelectedFileBatchAsync(
            selectedPaths.Select(path => new FileInfo(path)).Where(file => file.Exists).ToArray(),
            FileTransferKind.MultipleFiles,
            "多文件",
            rootFolderPath: null);
    }

    [RelayCommand]
    private async Task AttachFolderAsync()
    {
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            StatusMessage = "请先选择一个用户或群组再发送文件夹。";
            return;
        }

        var selectedPath = await PickFolderAsync();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        var directoryInfo = new DirectoryInfo(selectedPath);
        if (!directoryInfo.Exists)
        {
            StatusMessage = "文件夹不存在。";
            return;
        }

        await SendSelectedFolderAsync(directoryInfo);
    }

    [RelayCommand]
    private async Task AttachImageAsync()
    {
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId)
        {
            StatusMessage = "请先选择一个用户或群组再发送图片。";
            return;
        }

        var selectedPath = await PickImageAsync();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        if (!IsImageFileName(selectedPath))
        {
            StatusMessage = "请选择 PNG、JPG、JPEG、GIF、BMP 或 WebP 图片。";
            return;
        }

        await SendSelectedFileAsync(selectedPath, isImage: true);
    }

    private async Task SendSelectedFileAsync(string selectedPath, bool isImage)
    {
        if (SelectedUser is null)
        {
            return;
        }

        var fileInfo = new FileInfo(selectedPath);
        if (!fileInfo.Exists)
        {
            StatusMessage = "文件不存在。";
            return;
        }

        if (SelectedUser.IsGroupSession)
        {
            await SendSelectedGroupFileAsync(SelectedUser, selectedPath, fileInfo, isImage);
            return;
        }

        var fileId = Guid.NewGuid().ToString("N");
        var receiver = ToUserInfo(SelectedUser);
        var request = new FileTransferRequest(fileId, fileInfo.Name, fileInfo.Length, _settings.UserId, receiver.UserId, _settings.FilePort, isImage);
        var fileMessage = new ChatMessageViewModel
        {
            MessageId = fileId,
            SessionId = receiver.UserId,
            SenderId = _settings.UserId,
            ReceiverId = receiver.UserId,
            SenderName = LocalNickname,
            Content = isImage ? string.Empty : "等待接收方确认文件请求。",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            IsMine = true,
            Kind = isImage ? MessageKind.Image : MessageKind.File,
            FileName = fileInfo.Name,
            FileSizeText = FormatFileSize(fileInfo.Length),
            Progress = 0,
            StatusText = "等待确认"
        };

        if (isImage)
        {
            fileMessage.SetImagePath(selectedPath);
        }

        if (_settings.SaveChatHistory && isImage)
        {
            await _chatHistoryService.SaveMessageAsync(CreateImageChatMessage(fileId, receiver.UserId, fileInfo, selectedPath, isMine: true, readTargetCount: 1));
        }

        if (IsMessageSearchActive() && isImage)
        {
            await SearchSessionMessagesAsync(SelectedUser, MessageSearchText);
        }
        else
        {
            Messages.Add(fileMessage);
        }

        _outgoingFilePaths[fileId] = selectedPath;
        _outgoingFileReceivers[fileId] = receiver;
        _outgoingFileMessages[fileId] = fileMessage;
        UpsertRecentSession(SelectedUser, isImage ? $"[图片] {fileInfo.Name}" : $"[文件] {fileInfo.Name}", moveToTop: true);

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = fileId,
            SenderId = _settings.UserId,
            ReceiverId = receiver.UserId,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            SavePath = selectedPath,
            Status = FileTransferStatus.Pending,
            TransferTime = DateTimeOffset.Now
        });

        if (receiver.Status != UserStatus.Online)
        {
            await QueueFileDeliveryAsync(receiver.UserId, request, selectedPath, "对方当前离线，等待重新上线后提醒。");
            fileMessage.StatusText = "对方离线，等待上线提醒";
            await SaveOutgoingFileStatusAsync(fileId, FileTransferStatus.OfflineQueued);
            StatusMessage = $"{(isImage ? "图片" : "文件")}已加入离线提醒队列：{fileInfo.Name}";
            return;
        }

        try
        {
            await _messageService.SendFileRequestAsync(_settings, receiver, request);
            StatusMessage = isImage
                ? "图片请求已发送。接收方同意后开始传输。"
                : "文件请求已发送。接收方同意后开始传输。";
            fileMessage.StatusText = "请求已发送";
        }
        catch (Exception ex)
        {
            _logger.Error(isImage ? "图片请求发送失败。" : "文件请求发送失败。", ex);
            await QueueFileDeliveryAsync(receiver.UserId, request, selectedPath, ex.Message);
            fileMessage.StatusText = "发送失败，等待上线提醒";
            await SaveOutgoingFileStatusAsync(fileId, FileTransferStatus.OfflineQueued);
            StatusMessage = isImage ? $"图片请求发送失败，已加入离线提醒队列：{ex.Message}" : $"文件请求发送失败，已加入离线提醒队列：{ex.Message}";
        }
    }

    private async Task SendSelectedFolderAsync(DirectoryInfo directoryInfo)
    {
        try
        {
            var files = directoryInfo
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var directories = directoryInfo
                .EnumerateDirectories("*", SearchOption.AllDirectories)
                .OrderBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            await SendSelectedFileBatchAsync(files, FileTransferKind.Folder, directoryInfo.Name, directoryInfo.FullName, directories);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error("读取文件夹失败。", ex);
            StatusMessage = $"读取文件夹失败：{ex.Message}";
        }
    }

    private async Task SendSelectedFileBatchAsync(
        IReadOnlyList<FileInfo> files,
        FileTransferKind transferKind,
        string batchName,
        string? rootFolderPath,
        IReadOnlyList<DirectoryInfo>? directories = null)
    {
        if (SelectedUser is null)
        {
            return;
        }

        var localItems = CreateLocalTransferItems(files, transferKind, rootFolderPath, directories);
        var fileCount = localItems.Count(item => !item.IsDirectory);
        if (fileCount == 0 && transferKind != FileTransferKind.Folder)
        {
            StatusMessage = "没有可发送的文件。";
            return;
        }

        if (transferKind == FileTransferKind.Folder && fileCount == 0 && localItems.Count == 0)
        {
            StatusMessage = "文件夹为空，将发送空文件夹。";
        }

        if (SelectedUser.IsGroupSession)
        {
            await SendSelectedGroupBatchAsync(SelectedUser, localItems, transferKind, batchName, rootFolderPath);
            return;
        }

        await SendSelectedUserBatchAsync(SelectedUser, localItems, transferKind, batchName);
    }

    private async Task SendSelectedUserBatchAsync(
        OnlineUserViewModel selectedUser,
        IReadOnlyList<LocalTransferItem> localItems,
        FileTransferKind transferKind,
        string batchName)
    {
        var receiver = ToUserInfo(selectedUser);
        var batchId = Guid.NewGuid().ToString("N");
        var fileItems = localItems.Select(item => item.ToTransferItem()).ToArray();
        var files = localItems.Where(item => !item.IsDirectory).ToArray();
        var totalSize = files.Sum(item => item.FileSize);
        var displayName = BuildBatchDisplayName(transferKind, batchName, files.Length);
        var fileMessage = CreateBatchMessage(displayName, transferKind, files.Length, totalSize, isMine: true);
        fileMessage.MessageId = batchId;
        fileMessage.SessionId = receiver.UserId;
        fileMessage.SenderId = _settings.UserId;
        fileMessage.ReceiverId = receiver.UserId;
        var state = new FileBatchTransferState(fileMessage, files.Length, totalSize, transferKind, isSender: true);

        Messages.Add(fileMessage);
        UpsertRecentSession(selectedUser, BuildBatchRecentText(transferKind, displayName), moveToTop: true);

        await SaveBatchRecordAsync(batchId, receiver.UserId, displayName, totalSize, transferKind, batchId, null, FileTransferStatus.Pending);
        var batchItems = new List<OutgoingBatchItem>();
        foreach (var item in files)
        {
            RegisterOutgoingBatchItem(item, receiver, fileMessage, state);
            batchItems.Add(new OutgoingBatchItem(item.FileId, item.SourcePath, item.FileName, item.RelativePath, item.FileSize));
            await SaveBatchRecordAsync(item.FileId, receiver.UserId, item.FileName, item.FileSize, transferKind, batchId, item.RelativePath, FileTransferStatus.Pending);
        }

        _outgoingFileMessages[batchId] = fileMessage;
        _outgoingFileReceivers[batchId] = receiver;
        _outgoingBatchTransfers[batchId] = new OutgoingBatchTransfer(batchId, receiver, batchItems, fileMessage, state, transferKind);

        var request = new FileTransferRequest(
            batchId,
            displayName,
            totalSize,
            _settings.UserId,
            receiver.UserId,
            _settings.FilePort,
            IsImage: false,
            TransferKind: transferKind,
            BatchId: batchId,
            BatchName: displayName,
            Items: fileItems);

        var sourceMap = BuildSourceMap(localItems);
        if (receiver.Status != UserStatus.Online)
        {
            await QueueFileDeliveryAsync(receiver.UserId, request, sourceMap, "对方当前离线，等待重新上线后提醒。");
            fileMessage.StatusText = "对方离线，等待上线提醒";
            await SaveBatchRecordAsync(batchId, receiver.UserId, displayName, totalSize, transferKind, batchId, null, FileTransferStatus.OfflineQueued);
            StatusMessage = $"{GetTransferKindText(transferKind)}已加入离线提醒队列：{displayName}";
            return;
        }

        try
        {
            await _messageService.SendFileRequestAsync(_settings, receiver, request);
            fileMessage.StatusText = "批量请求已发送";
            StatusMessage = $"{GetTransferKindText(transferKind)}请求已发送。接收方同意后开始传输。";
        }
        catch (Exception ex)
        {
            _logger.Error($"{GetTransferKindText(transferKind)}请求发送失败。", ex);
            await QueueFileDeliveryAsync(receiver.UserId, request, sourceMap, ex.Message);
            fileMessage.StatusText = "发送失败，等待上线提醒";
            await SaveBatchRecordAsync(batchId, receiver.UserId, displayName, totalSize, transferKind, batchId, null, FileTransferStatus.OfflineQueued);
            StatusMessage = $"{GetTransferKindText(transferKind)}请求发送失败，已加入离线提醒队列：{ex.Message}";
        }
    }

    private async Task SendSelectedGroupBatchAsync(
        OnlineUserViewModel group,
        IReadOnlyList<LocalTransferItem> localItems,
        FileTransferKind transferKind,
        string batchName,
        string? rootFolderPath)
    {
        var memberIds = group.GroupMemberIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var recipientIds = memberIds
            .Where(id => id != _settings.UserId)
            .ToArray();
        if (recipientIds.Length == 0)
        {
            StatusMessage = $"群组“{group.Nickname}”没有其他成员可接收{GetTransferKindText(transferKind)}。";
            return;
        }

        var files = localItems.Where(item => !item.IsDirectory).ToArray();
        var totalSize = files.Sum(item => item.FileSize);
        var totalFileDeliveries = files.Length * recipientIds.Length;
        var totalBytes = totalSize * recipientIds.Length;
        var displayName = BuildBatchDisplayName(transferKind, batchName, files.Length);
        var groupMessageId = Guid.NewGuid().ToString("N");
        var fileMessage = CreateBatchMessage(displayName, transferKind, files.Length, totalSize, isMine: true);
        fileMessage.MessageId = groupMessageId;
        fileMessage.SessionId = group.UserId;
        fileMessage.SenderId = _settings.UserId;
        fileMessage.ReceiverId = group.UserId;
        fileMessage.StatusText = $"准备发送给 {recipientIds.Length} 个成员";
        var state = new FileBatchTransferState(fileMessage, totalFileDeliveries, totalBytes, transferKind, isSender: true);

        Messages.Add(fileMessage);
        UpsertRecentSession(group, BuildBatchRecentText(transferKind, displayName), moveToTop: true);

        var onlineReceivers = ResolveGroupReceivers(group)
            .Where(user => user.Status == UserStatus.Online)
            .ToDictionary(user => user.UserId, StringComparer.Ordinal);
        var success = 0;
        var queued = 0;
        var failed = 0;

        foreach (var recipientId in recipientIds)
        {
            var batchId = Guid.NewGuid().ToString("N");
            var clonedItems = CloneTransferItemsForRecipient(localItems);
            var sourceMap = BuildSourceMap(clonedItems);
            var request = new FileTransferRequest(
                batchId,
                displayName,
                totalSize,
                _settings.UserId,
                recipientId,
                _settings.FilePort,
                IsImage: false,
                GroupId: group.UserId,
                GroupName: group.Nickname,
                GroupKind: group.GroupKind,
                GroupMemberUserIds: memberIds,
                GroupMessageId: groupMessageId,
                TransferKind: transferKind,
                BatchId: batchId,
                BatchName: displayName,
                Items: clonedItems.Select(item => item.ToTransferItem()).ToArray());

            var receiver = onlineReceivers.TryGetValue(recipientId, out var onlineReceiver)
                ? onlineReceiver
                : ResolveKnownUserInfo(recipientId);

            var outgoingItems = new List<OutgoingBatchItem>();
            foreach (var item in clonedItems.Where(item => !item.IsDirectory))
            {
                RegisterOutgoingBatchItem(item, receiver, fileMessage, state);
                outgoingItems.Add(new OutgoingBatchItem(item.FileId, item.SourcePath, item.FileName, item.RelativePath, item.FileSize));
                await SaveBatchRecordAsync(item.FileId, recipientId, item.FileName, item.FileSize, transferKind, batchId, item.RelativePath, FileTransferStatus.Pending);
            }

            _outgoingFileMessages[batchId] = fileMessage;
            _outgoingFileReceivers[batchId] = receiver;
            _outgoingBatchTransfers[batchId] = new OutgoingBatchTransfer(batchId, receiver, outgoingItems, fileMessage, state, transferKind);

            if (!onlineReceivers.TryGetValue(recipientId, out var onlineUser))
            {
                await QueueGroupBatchDeliveryAsync(recipientId, request, sourceMap, "成员当前离线，等待重新上线后补发。");
                queued++;
                continue;
            }

            try
            {
                await _messageService.SendFileRequestAsync(_settings, onlineUser, request);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                await QueueGroupBatchDeliveryAsync(recipientId, request, sourceMap, ex.Message);
                _logger.Warning($"群组{GetTransferKindText(transferKind)}请求发送给 {onlineUser.Nickname}({onlineUser.IpAddress}) 失败，已加入离线补发队列：{ex.Message}");
            }
        }

        state.Apply();
        StatusMessage = queued == 0 && failed == 0
            ? $"群组{GetTransferKindText(transferKind)}请求已发送给 {success} 个在线成员。"
            : $"群组{GetTransferKindText(transferKind)}请求已发送 {success} 人，离线补发 {queued + failed} 人。";
    }

    private async Task SendSelectedGroupFileAsync(OnlineUserViewModel group, string selectedPath, FileInfo fileInfo, bool isImage)
    {
        var memberIds = group.GroupMemberIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var recipientIds = memberIds
            .Where(id => id != _settings.UserId)
            .ToArray();
        if (recipientIds.Length == 0)
        {
            StatusMessage = $"群组“{group.Nickname}”没有其他成员可接收{(isImage ? "图片" : "文件")}。";
            return;
        }

        var onlineReceivers = ResolveGroupReceivers(group)
            .Where(user => user.Status == UserStatus.Online)
            .ToDictionary(user => user.UserId, StringComparer.Ordinal);

        var groupMessageId = Guid.NewGuid().ToString("N");
        var fileMessage = new ChatMessageViewModel
        {
            MessageId = groupMessageId,
            SessionId = group.UserId,
            SenderId = _settings.UserId,
            ReceiverId = group.UserId,
            SenderName = LocalNickname,
            Content = isImage ? string.Empty : $"群组文件：{fileInfo.Name}",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            IsMine = true,
            Kind = isImage ? MessageKind.Image : MessageKind.File,
            FileName = fileInfo.Name,
            FileSizeText = FormatFileSize(fileInfo.Length),
            Progress = 0,
            StatusText = $"准备群发给 {recipientIds.Length} 个成员"
        };

        if (isImage)
        {
            fileMessage.SetImagePath(selectedPath);
        }

        if (_settings.SaveChatHistory && isImage)
        {
            await _chatHistoryService.SaveMessageAsync(CreateImageChatMessage(
                groupMessageId,
                group.UserId,
                fileInfo,
                selectedPath,
                isMine: true,
                sessionId: group.UserId,
                receiverId: group.UserId,
                readTargetCount: recipientIds.Length));
        }

        if (IsMessageSearchActive() && isImage)
        {
            await SearchSessionMessagesAsync(group, MessageSearchText);
        }
        else
        {
            Messages.Add(fileMessage);
        }

        var state = new GroupFileTransferState(fileMessage, recipientIds.Length);
        UpsertRecentSession(group, isImage ? $"[群组图片] {fileInfo.Name}" : $"[群组文件] {fileInfo.Name}", moveToTop: true);

        var success = 0;
        var queued = 0;
        var failure = 0;
        foreach (var recipientId in recipientIds)
        {
            var fileId = Guid.NewGuid().ToString("N");
            var request = new FileTransferRequest(
                fileId,
                fileInfo.Name,
                fileInfo.Length,
                _settings.UserId,
                recipientId,
                _settings.FilePort,
                isImage,
                group.UserId,
                group.Nickname,
                group.GroupKind,
                memberIds,
                groupMessageId);

            _outgoingFilePaths[fileId] = selectedPath;
            _outgoingFileMessages[fileId] = fileMessage;
            _outgoingGroupFileStates[fileId] = state;

            var knownReceiver = onlineReceivers.TryGetValue(recipientId, out var onlineReceiver)
                ? onlineReceiver
                : ResolveKnownUserInfo(recipientId);
            _outgoingFileReceivers[fileId] = knownReceiver;

            await _fileTransferRepository.SaveAsync(new FileTransferRecord
            {
                FileId = fileId,
                SenderId = _settings.UserId,
                ReceiverId = recipientId,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                SavePath = selectedPath,
                Status = FileTransferStatus.Pending,
                TransferTime = DateTimeOffset.Now
            });

            if (!onlineReceivers.TryGetValue(recipientId, out var receiver))
            {
                await QueueGroupFileDeliveryAsync(recipientId, request, selectedPath, "成员当前离线，等待重新上线后补发。");
                state.MarkQueued(fileId);
                queued++;
                continue;
            }

            try
            {
                await _messageService.SendFileRequestAsync(_settings, receiver, request);
                state.MarkRequestSent(fileId);
                success++;
            }
            catch (Exception ex)
            {
                failure++;
                await QueueGroupFileDeliveryAsync(recipientId, request, selectedPath, ex.Message);
                state.MarkQueued(fileId);
                _logger.Warning($"群组{(isImage ? "图片" : "文件")}请求发送给 {receiver.Nickname}({receiver.IpAddress}) 失败，已加入离线补发队列：{ex.Message}");
            }
        }

        state.Apply();
        StatusMessage = queued == 0 && failure == 0
            ? $"群组{(isImage ? "图片" : "文件")}请求已发送给 {success} 个在线成员。"
            : $"群组{(isImage ? "图片" : "文件")}请求已发送 {success} 人，离线补发 {queued + failure} 人。";
    }

    private static string? ValidatePorts(SettingsViewModel settings)
    {
        if (!IsUserPort(settings.UdpPort) ||
            !IsUserPort(settings.MessagePort) ||
            !IsUserPort(settings.FilePort))
        {
            return "端口必须在 1024 到 65535 之间。";
        }

        if (settings.UdpPort == settings.MessagePort ||
            settings.UdpPort == settings.FilePort ||
            settings.MessagePort == settings.FilePort)
        {
            return "UDP 自动发现、TCP 消息、TCP 文件端口不能重复。";
        }

        return null;
    }

    private static bool IsUserPort(int port)
    {
        return port is >= 1024 and <= 65535;
    }

    private static void EnsurePortsAvailable(AppSettings settings)
    {
        EnsureUdpPortAvailable(settings.UdpPort);
        EnsureTcpPortAvailable(settings.MessagePort, "TCP 消息");
        EnsureTcpPortAvailable(settings.FilePort, "TCP 文件");
    }

    private static void EnsureUdpPortAvailable(int port)
    {
        try
        {
            using var udpClient = new UdpClient(port);
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException($"UDP 自动发现端口 {port} 不可用：{ex.SocketErrorCode}", ex);
        }
    }

    private static void EnsureTcpPortAvailable(int port, string name)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException($"{name}端口 {port} 不可用：{ex.SocketErrorCode}", ex);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            _settings = await _settingsService.LoadAsync();
            AppThemeService.Apply(_settings);
            LocalNickname = _settings.Nickname;
            OnPropertyChanged(nameof(LocalAvatarBrush));
            LocalDepartment = _settings.Department;
            LocalIpAddress = NetworkInterfaceHelper.GetLocalIpAddress();
            LocalStatusText = "在线";
            Settings = SettingsViewModel.FromSettings(_settings);

            var initializer = new DatabaseInitializer(new SqliteConnectionFactory());
            await initializer.InitializeAsync();
            await LoadKnownUsersAsync();
            await LoadPermanentGroupsAsync();
            EnsurePortsAvailable(_settings);
            await _discoveryService.StartAsync(_settings);
            await _messageService.StartAsync(_settings);
            StartFileServer();
            _logger.Info("程序启动，本机配置与数据库已加载。");
            StatusMessage = "本机设置已加载，UDP 自动发现与 TCP 消息监听已启动。";
        }
        catch (Exception ex)
        {
            _logger.Error("初始化失败。", ex);
            LocalStatusText = "异常";
            StatusMessage = $"初始化失败：{ex.Message}";
        }
    }

    public async Task ShutdownAsync()
    {
        _discoveryService.UsersChanged -= OnDiscoveryUsersChanged;
        _messageService.PacketReceived -= OnMessagePacketReceived;
        if (_fileServerCts is not null)
        {
            await _fileServerCts.CancelAsync().ConfigureAwait(false);
            _fileServerCts.Dispose();
            _fileServerCts = null;
        }

        if (_messageSearchCts is not null)
        {
            await _messageSearchCts.CancelAsync().ConfigureAwait(false);
            _messageSearchCts.Dispose();
            _messageSearchCts = null;
        }

        await _messageService.StopAsync().ConfigureAwait(false);
        await _discoveryService.StopAsync().ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() => LocalStatusText = "离线");
    }

    private void OnDiscoveryUsersChanged(object? sender, IReadOnlyCollection<UserInfo> users)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var user in users)
            {
                var viewModel = UpsertOnlineUser(user);
                if (viewModel.Status == UserStatus.Online && viewModel.LastMessage is "等待重新上线" or "已离线")
                {
                    viewModel.LastMessage = "可以开始聊天";
                }

                UpsertRecentSession(viewModel, viewModel.LastMessage, moveToTop: false);
                if (viewModel.Status == UserStatus.Online)
                {
                    _ = RetryPendingDeliveriesAsync(viewModel);
                }
            }

            OnPropertyChanged(nameof(OnlineCount));
            RefreshFilteredUsers();
            _ = PersistKnownUsersAsync(users);

            if (OnlineCount == 0)
            {
                StatusMessage = "UDP 自动发现已启动，暂未发现其他在线用户。";
            }
            else
            {
                StatusMessage = $"已发现 {OnlineCount} 个在线局域网用户。";
            }
        });
    }

    private void OnMessagePacketReceived(object? sender, NetworkPacket packet)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await HandleIncomingPacketAsync(packet);
            }
            catch (Exception ex)
            {
                _logger.Error("处理收到的消息失败。", ex);
                StatusMessage = $"处理收到的消息失败：{ex.Message}";
            }
        });
    }

    private async Task HandleIncomingPacketAsync(NetworkPacket packet)
    {
        if (packet.Type is PacketType.FileRequest)
        {
            await HandleFileRequestAsync(packet);
            return;
        }

        if (packet.Type is PacketType.FileAccept or PacketType.FileReject)
        {
            await HandleFileResponseAsync(packet);
            return;
        }

        if (packet.Type is PacketType.FileFinished)
        {
            await HandleFileFinishedAsync(packet);
            return;
        }

        if (packet.Type is PacketType.Error)
        {
            await HandleErrorAsync(packet);
            return;
        }

        if (packet.Type is PacketType.MessageReadReceipt)
        {
            await HandleReadReceiptAsync(packet);
            return;
        }

        if (packet.Type is PacketType.MessageRecall)
        {
            await HandleMessageRecallAsync(packet);
            return;
        }

        if (packet.Type is PacketType.OfflineFileReminder)
        {
            await HandleOfflineFileReminderAsync(packet);
            return;
        }

        if (packet.Type == PacketType.GroupMessage)
        {
            await HandleGroupMessageAsync(packet);
            return;
        }

        if (packet.Type is not (PacketType.PrivateMessage or PacketType.BroadcastMessage))
        {
            return;
        }

        var payload = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.TextMessagePayload);
        if (payload is null)
        {
            return;
        }

        var kind = packet.Type == PacketType.BroadcastMessage ? MessageKind.Broadcast : MessageKind.Private;
        var sessionId = kind == MessageKind.Broadcast ? NetworkConstants.BroadcastSessionId : packet.FromUserId;
        var senderName = ResolveUserName(packet.FromUserId);
        var session = kind == MessageKind.Broadcast
            ? EnsureBroadcastSession()
            : EnsureUserSession(packet.FromUserId, senderName, "0.0.0.0");
        var message = new ChatMessage
        {
            MessageId = payload.MessageId,
            SessionId = sessionId,
            SenderId = packet.FromUserId,
            ReceiverId = packet.ToUserId,
            Kind = kind,
            Content = payload.Content,
            SendTime = packet.Time,
            IsMine = false
        };

        if (_settings.SaveChatHistory)
        {
            await _chatHistoryService.SaveMessageAsync(message);
        }

        var isCurrentSession = SelectedUser?.UserId == sessionId ||
            (kind == MessageKind.Broadcast && SelectedUser?.UserId == NetworkConstants.BroadcastSessionId);

        if (isCurrentSession && SelectedUser is not null)
        {
            if (IsMessageSearchActive())
            {
                await SearchSessionMessagesAsync(SelectedUser, MessageSearchText);
            }
            else
            {
                Messages.Add(ToMessageViewModel(message, senderName));
            }
        }
        else
        {
            session.UnreadCount++;
            RefreshUnreadState();
        }

        if (isCurrentSession && kind == MessageKind.Private)
        {
            await MarkIncomingMessageReadAsync(message, isGroup: false);
        }

        UpsertRecentSession(session, payload.Content, moveToTop: true);
        RefreshUnreadState();
        StatusMessage = kind == MessageKind.Broadcast ? "收到一条广播消息。" : $"收到来自 {senderName} 的消息。";
        RequestNotification(
            kind == MessageKind.Broadcast ? "收到广播消息" : $"收到 {senderName} 的消息",
            payload.Content,
            sessionId);
    }

    private async Task HandleGroupMessageAsync(NetworkPacket packet)
    {
        var payload = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.GroupMessagePayload);
        if (payload is null || string.IsNullOrWhiteSpace(payload.GroupId))
        {
            return;
        }

        var memberIds = payload.MemberUserIds
            .Append(_settings.UserId)
            .Append(packet.FromUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var group = new ChatGroup
        {
            GroupId = payload.GroupId,
            Name = string.IsNullOrWhiteSpace(payload.GroupName) ? "未命名群组" : payload.GroupName,
            Kind = payload.GroupKind,
            MemberUserIds = memberIds,
            CreatedTime = DateTimeOffset.Now,
            UpdatedTime = DateTimeOffset.Now
        };
        var session = EnsureGroupSession(group);
        if (group.Kind == GroupKind.Permanent)
        {
            await _groupRepository.SaveAsync(group);
        }

        var senderName = string.IsNullOrWhiteSpace(payload.SenderNickname)
            ? ResolveUserName(packet.FromUserId)
            : payload.SenderNickname;
        var message = new ChatMessage
        {
            MessageId = payload.MessageId,
            SessionId = payload.GroupId,
            SenderId = packet.FromUserId,
            ReceiverId = payload.GroupId,
            Kind = MessageKind.Group,
            Content = payload.Content,
            SendTime = packet.Time,
            IsMine = false
        };

        if (_settings.SaveChatHistory)
        {
            await _chatHistoryService.SaveMessageAsync(message);
        }

        if (SelectedUser?.UserId == payload.GroupId)
        {
            if (IsMessageSearchActive())
            {
                await SearchSessionMessagesAsync(session, MessageSearchText);
            }
            else
            {
                Messages.Add(ToMessageViewModel(message, senderName));
            }
        }
        else
        {
            session.UnreadCount++;
            RefreshUnreadState();
        }

        if (SelectedUser?.UserId == payload.GroupId)
        {
            await MarkIncomingMessageReadAsync(message, isGroup: true);
        }

        UpsertRecentSession(session, $"{senderName}: {payload.Content}", moveToTop: true);
        RefreshUnreadState();
        StatusMessage = $"收到群组“{session.Nickname}”的新消息。";
        RequestNotification($"群组 {session.Nickname}", $"{senderName}: {payload.Content}", payload.GroupId);
    }

    private async Task HandleFileRequestAsync(NetworkPacket packet)
    {
        var request = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.FileTransferRequest);
        if (request is null)
        {
            return;
        }

        if (request.IsBatchTransfer)
        {
            await HandleBatchFileRequestAsync(packet, request);
            return;
        }

        var isImage = request.IsImage || IsImageFileName(request.FileName);
        _pendingFileRequests[request.FileId] = request;
        var sender = ResolveUserName(packet.FromUserId);
        var session = request.IsGroupTransfer
            ? EnsureGroupSession(new ChatGroup
            {
                GroupId = request.GroupId!,
                Name = string.IsNullOrWhiteSpace(request.GroupName) ? "未命名群组" : request.GroupName,
                Kind = request.GroupKind,
                MemberUserIds = (request.GroupMemberUserIds ?? [])
                    .Append(_settings.UserId)
                    .Append(request.SenderId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                CreatedTime = DateTimeOffset.Now,
                UpdatedTime = DateTimeOffset.Now
            })
            : EnsureUserSession(packet.FromUserId, sender, "0.0.0.0");
        if (request.IsGroupTransfer && request.GroupKind == GroupKind.Permanent)
        {
            await _groupRepository.SaveAsync(new ChatGroup
            {
                GroupId = request.GroupId!,
                Name = string.IsNullOrWhiteSpace(request.GroupName) ? "未命名群组" : request.GroupName,
                Kind = request.GroupKind,
                MemberUserIds = session.GroupMemberIds.ToArray(),
                CreatedTime = DateTimeOffset.Now,
                UpdatedTime = DateTimeOffset.Now
            });
        }

        var savePath = Path.Combine(_settings.FileSavePath, ToSafePathSegment(request.FileName));

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SavePath = savePath,
            Status = FileTransferStatus.Pending,
            TransferTime = DateTimeOffset.Now
        });

        if (_settings.SaveChatHistory && isImage)
        {
            await _chatHistoryService.SaveMessageAsync(CreateImageChatMessage(
                request.FileId,
                request.SenderId,
                new FileInfo(savePath),
                savePath,
                isMine: false,
                sessionId: request.GroupId ?? request.SenderId,
                senderId: request.SenderId,
                receiverId: request.GroupId ?? _settings.UserId,
                fileName: request.FileName,
                fileSize: request.FileSize));
        }

        var fileMessage = new ChatMessageViewModel
        {
            MessageId = request.FileId,
            SessionId = session.UserId,
            SenderId = request.SenderId,
            ReceiverId = request.GroupId ?? _settings.UserId,
            SenderName = sender,
            Content = isImage ? string.Empty : request.IsGroupTransfer ? $"群组文件：{request.FileName}" : "收到文件发送请求，请确认是否接收。",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            Kind = isImage ? MessageKind.Image : MessageKind.File,
            FileName = request.FileName,
            FileSizeText = FormatFileSize(request.FileSize),
            Progress = 0,
            StatusText = "等待接收确认"
        };

        if (isImage)
        {
            fileMessage.SetImagePath(savePath);
        }

        if (SelectedUser?.UserId == session.UserId)
        {
            Messages.Add(fileMessage);
        }
        else
        {
            session.UnreadCount++;
            RefreshUnreadState();
        }

        UpsertRecentSession(session, isImage ? $"[图片] {request.FileName}" : $"收到文件：{request.FileName}", moveToTop: true);
        RefreshUnreadState();
        _incomingFileMessages[request.FileId] = fileMessage;
        _incomingSavePaths[request.FileId] = savePath;
        PendingFileRequest = new FileReceiveRequestViewModel
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            SenderName = sender,
            FileName = request.FileName,
            FileSizeText = FormatFileSize(request.FileSize),
            IsImage = isImage,
            GroupId = request.GroupId ?? string.Empty,
            GroupName = request.GroupName ?? string.Empty
        };
        IsFileRequestPaneOpen = true;
        StatusMessage = isImage
            ? request.IsGroupTransfer ? $"群组“{session.Nickname}”收到 {sender} 的图片：{request.FileName}" : $"收到 {sender} 的图片：{request.FileName}"
            : request.IsGroupTransfer ? $"群组“{session.Nickname}”收到 {sender} 的文件请求：{request.FileName}" : $"收到 {sender} 的文件请求：{request.FileName}";
        RequestNotification(
            isImage ? "收到图片" : "收到文件请求",
            request.IsGroupTransfer
                ? $"{sender} 在群组“{session.Nickname}”发来{(isImage ? "图片" : "文件")}：{request.FileName}"
                : isImage ? $"{sender} 发来图片：{request.FileName}" : $"{sender} 想发送：{request.FileName}",
            session.UserId);
    }

    private async Task HandleBatchFileRequestAsync(NetworkPacket packet, FileTransferRequest request)
    {
        var sender = ResolveUserName(packet.FromUserId);
        var session = request.IsGroupTransfer
            ? EnsureGroupSession(new ChatGroup
            {
                GroupId = request.GroupId!,
                Name = string.IsNullOrWhiteSpace(request.GroupName) ? "未命名群组" : request.GroupName,
                Kind = request.GroupKind,
                MemberUserIds = (request.GroupMemberUserIds ?? [])
                    .Append(_settings.UserId)
                    .Append(request.SenderId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                CreatedTime = DateTimeOffset.Now,
                UpdatedTime = DateTimeOffset.Now
            })
            : EnsureUserSession(packet.FromUserId, sender, "0.0.0.0");
        if (request.IsGroupTransfer && request.GroupKind == GroupKind.Permanent)
        {
            await _groupRepository.SaveAsync(new ChatGroup
            {
                GroupId = request.GroupId!,
                Name = string.IsNullOrWhiteSpace(request.GroupName) ? "未命名群组" : request.GroupName,
                Kind = request.GroupKind,
                MemberUserIds = session.GroupMemberIds.ToArray(),
                CreatedTime = DateTimeOffset.Now,
                UpdatedTime = DateTimeOffset.Now
            });
        }

        var items = request.TransferItems.ToArray();
        var fileItems = items.Where(item => !item.IsDirectory).ToArray();
        var totalSize = fileItems.Sum(item => item.FileSize);
        var displayName = string.IsNullOrWhiteSpace(request.BatchName) ? request.FileName : request.BatchName;
        var saveRoot = BuildIncomingBatchRootPath(request, displayName);

        foreach (var item in items)
        {
            if (!TryNormalizeTransferRelativePath(item.RelativePath, item.IsDirectory, out _, out var error))
            {
                var senderUser = OnlineUsers.FirstOrDefault(user => user.UserId == request.SenderId);
                if (senderUser is not null)
                {
                    await _messageService.SendFileResponseAsync(
                        _settings,
                        ToUserInfo(senderUser),
                        new FileTransferResponse(request.FileId, false, $"包含不安全路径，已拒绝：{error}"));
                }

                StatusMessage = $"{GetTransferKindText(request.TransferKind)}包含不安全路径，已拒绝。";
                return;
            }
        }

        await SaveBatchRecordAsync(
            request.FileId,
            request.SenderId,
            request.ReceiverId,
            displayName,
            totalSize,
            request.TransferKind,
            request.FileId,
            null,
            FileTransferStatus.Pending,
            saveRoot);

        var fileMessage = CreateBatchMessage(displayName, request.TransferKind, fileItems.Length, totalSize, isMine: false, sender);
        fileMessage.MessageId = request.FileId;
        fileMessage.SessionId = session.UserId;
        fileMessage.SenderId = request.SenderId;
        fileMessage.ReceiverId = request.GroupId ?? _settings.UserId;
        var state = new FileBatchTransferState(fileMessage, fileItems.Length, totalSize, request.TransferKind, isSender: false);

        if (SelectedUser?.UserId == session.UserId)
        {
            Messages.Add(fileMessage);
        }
        else
        {
            session.UnreadCount++;
            RefreshUnreadState();
        }

        UpsertRecentSession(session, BuildIncomingBatchRecentText(request.TransferKind, displayName), moveToTop: true);
        RefreshUnreadState();

        _pendingFileRequests[request.FileId] = request;
        _incomingFileMessages[request.FileId] = fileMessage;
        _incomingSavePaths[request.FileId] = saveRoot;
        _incomingBatchStates[request.FileId] = state;

        foreach (var item in items)
        {
            TryNormalizeTransferRelativePath(item.RelativePath, item.IsDirectory, out var safeRelativePath, out _);
            var itemSavePath = Path.Combine(saveRoot, safeRelativePath);
            if (item.IsDirectory)
            {
                continue;
            }

            var itemRequest = request with
            {
                FileId = item.FileId,
                FileName = item.FileName,
                FileSize = item.FileSize,
                RelativePath = safeRelativePath,
                Items = null
            };
            _pendingFileRequests[item.FileId] = itemRequest;
            _incomingFileMessages[item.FileId] = fileMessage;
            _incomingSavePaths[item.FileId] = itemSavePath;
            _incomingBatchStates[item.FileId] = state;
            await SaveBatchRecordAsync(
                item.FileId,
                request.SenderId,
                item.FileName,
                item.FileSize,
                request.TransferKind,
                request.FileId,
                safeRelativePath,
                FileTransferStatus.Pending,
                itemSavePath);
        }

        PendingFileRequest = new FileReceiveRequestViewModel
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            SenderName = sender,
            FileName = displayName,
            FileSizeText = BuildBatchSizeText(fileItems.Length, totalSize),
            TransferKind = request.TransferKind,
            ItemCount = fileItems.Length,
            IsImage = false,
            GroupId = request.GroupId ?? string.Empty,
            GroupName = request.GroupName ?? string.Empty
        };
        IsFileRequestPaneOpen = true;
        StatusMessage = request.IsGroupTransfer
            ? $"群组“{session.Nickname}”收到 {sender} 的{GetTransferKindText(request.TransferKind)}请求：{displayName}"
            : $"收到 {sender} 的{GetTransferKindText(request.TransferKind)}请求：{displayName}";
        RequestNotification(
            $"收到{GetTransferKindText(request.TransferKind)}",
            request.IsGroupTransfer
                ? $"{sender} 在群组“{session.Nickname}”发来{GetTransferKindText(request.TransferKind)}：{displayName}"
                : $"{sender} 想发送{GetTransferKindText(request.TransferKind)}：{displayName}",
            session.UserId);
    }

    private async Task AcceptBatchFileRequestAsync(FileTransferRequest request, OnlineUserViewModel sender, ChatMessageViewModel fileMessage)
    {
        var saveRoot = _incomingSavePaths.GetValueOrDefault(request.FileId) ?? BuildIncomingBatchRootPath(request, request.FileName);
        Directory.CreateDirectory(saveRoot);

        var resumeItems = new List<FileTransferResumeItem>();
        var state = _incomingBatchStates.GetValueOrDefault(request.FileId);
        foreach (var item in request.TransferItems)
        {
            TryNormalizeTransferRelativePath(item.RelativePath, item.IsDirectory, out var safeRelativePath, out _);
            var savePath = Path.Combine(saveRoot, safeRelativePath);
            if (item.IsDirectory)
            {
                Directory.CreateDirectory(savePath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? saveRoot);
            var resumeOffset = GetResumeOffset(savePath, item.FileSize);
            resumeItems.Add(new FileTransferResumeItem(item.FileId, resumeOffset));
            state?.MarkProgress(item.FileId, item.FileSize == 0 ? 100 : resumeOffset * 100d / item.FileSize);
            await SaveBatchRecordAsync(
                item.FileId,
                request.SenderId,
                request.ReceiverId,
                item.FileName,
                item.FileSize,
                request.TransferKind,
                request.FileId,
                safeRelativePath,
                FileTransferStatus.Accepted,
                savePath,
                resumeOffset);
        }

        state?.Apply();
        fileMessage.StatusText = resumeItems.Any(item => item.Offset > 0)
            ? "已接受，等待断点续传"
            : "已接受，等待批量传输";
        await SaveBatchRecordAsync(
            request.FileId,
            request.SenderId,
            request.ReceiverId,
            request.FileName,
            request.FileSize,
            request.TransferKind,
            request.FileId,
            null,
            FileTransferStatus.Accepted,
            saveRoot,
            resumeItems.Sum(item => item.Offset));

        await _messageService.SendFileResponseAsync(
            _settings,
            ToUserInfo(sender),
            new FileTransferResponse(request.FileId, true, ResumeItems: resumeItems));
        StatusMessage = $"已接受{GetTransferKindText(request.TransferKind)}：{request.FileName}";
    }

    private async Task RejectBatchFileRequestAsync(FileTransferRequest request)
    {
        foreach (var item in request.TransferItems.Where(item => !item.IsDirectory))
        {
            await SaveBatchRecordAsync(
                item.FileId,
                request.SenderId,
                request.ReceiverId,
                item.FileName,
                item.FileSize,
                request.TransferKind,
                request.FileId,
                item.RelativePath,
                FileTransferStatus.Rejected,
                _incomingSavePaths.GetValueOrDefault(item.FileId));
        }

        await SaveBatchRecordAsync(
            request.FileId,
            request.SenderId,
            request.ReceiverId,
            request.FileName,
            request.FileSize,
            request.TransferKind,
            request.FileId,
            null,
            FileTransferStatus.Rejected,
            _incomingSavePaths.GetValueOrDefault(request.FileId));
    }

    private async Task HandleFileResponseAsync(NetworkPacket packet)
    {
        var response = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.FileTransferResponse);
        if (response is null)
        {
            return;
        }

        if (_outgoingBatchTransfers.TryGetValue(response.FileId, out var batchTransfer))
        {
            await HandleBatchFileResponseAsync(response, batchTransfer);
            return;
        }

        if (!_outgoingFileMessages.TryGetValue(response.FileId, out var fileMessage))
        {
            return;
        }

        if (!response.Accepted)
        {
            fileMessage.StatusText = response.Reason ?? "接收方已拒绝";
            fileMessage.Progress = 0;
            StatusMessage = $"文件被拒绝：{fileMessage.FileName}";
            if (_outgoingGroupFileStates.TryGetValue(response.FileId, out var rejectedGroupState))
            {
                rejectedGroupState.MarkRejected(response.FileId);
                rejectedGroupState.Apply();
                StatusMessage = $"群组附件被一名成员拒绝：{fileMessage.FileName}";
            }

            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Rejected);
            return;
        }

        if (!_outgoingFilePaths.TryGetValue(response.FileId, out var path) ||
            !_outgoingFileReceivers.TryGetValue(response.FileId, out var receiver))
        {
            fileMessage.StatusText = "文件路径丢失";
            if (_outgoingGroupFileStates.TryGetValue(response.FileId, out var missingPathGroupState))
            {
                missingPathGroupState.MarkFailed(response.FileId);
                missingPathGroupState.Apply();
            }

            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Failed);
            return;
        }

        var groupState = _outgoingGroupFileStates.GetValueOrDefault(response.FileId);
        groupState?.MarkAccepted(response.FileId);
        fileMessage.StatusText = groupState is null ? "正在传输" : groupState.BuildStatus();
        var sourceLength = new FileInfo(path).Exists ? new FileInfo(path).Length : 0;
        var resumeOffset = Math.Clamp(response.ResumeOffset, 0, sourceLength);
        await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Transferring, resumeOffset);

        try
        {
            var progress = new Progress<double>(value =>
            {
                if (groupState is null)
                {
                    fileMessage.Progress = value;
                    fileMessage.StatusText = $"正在传输 {value:0}%";
                    return;
                }

                fileMessage.StatusText = groupState.BuildStatus(value);
            });

            await _fileTransferService.SendFileAsync(receiver.IpAddress, receiver.FilePort, response.FileId, path, progress, resumeOffset);
            if (groupState is null)
            {
                fileMessage.Progress = 100;
                fileMessage.StatusText = "已发送，等待对方确认";
                StatusMessage = $"文件已发送，等待对方确认：{fileMessage.FileName}";
            }
            else
            {
                groupState.Apply();
                StatusMessage = $"群组附件已发送给一名成员，等待接收确认：{fileMessage.FileName}";
            }

            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Transferring, sourceLength);
        }
        catch (Exception ex)
        {
            _logger.Error("文件传输失败。", ex);
            fileMessage.StatusText = "传输失败";
            StatusMessage = $"文件传输失败：{ex.Message}";
            if (groupState is not null)
            {
                groupState.MarkFailed(response.FileId);
                groupState.Apply();
                StatusMessage = $"群组附件传输失败：{ex.Message}";
            }

            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Failed);

            try
            {
                await _messageService.SendErrorAsync(
                    _settings,
                    receiver,
                    new ErrorPayload("FILE_TRANSFER_FAILED", $"文件传输失败：{fileMessage.FileName}", response.FileId));
            }
            catch (Exception notifyEx)
            {
                _logger.Warning($"文件传输失败通知发送失败：{notifyEx.Message}");
            }
        }
    }

    private async Task HandleBatchFileResponseAsync(FileTransferResponse response, OutgoingBatchTransfer batchTransfer)
    {
        if (!response.Accepted)
        {
            if (batchTransfer.Items.Count == 0)
            {
                batchTransfer.State.MarkRejected(response.FileId);
            }
            else
            {
                foreach (var item in batchTransfer.Items)
                {
                    batchTransfer.State.MarkRejected(item.FileId);
                }
            }

            batchTransfer.State.Apply();
            batchTransfer.Message.StatusText = response.Reason ?? "接收方已拒绝";
            StatusMessage = $"{GetTransferKindText(batchTransfer.TransferKind)}被拒绝：{batchTransfer.Message.FileName}";
            await SaveBatchRecordAsync(
                batchTransfer.BatchId,
                batchTransfer.Receiver.UserId,
                batchTransfer.Message.FileName,
                batchTransfer.Items.Sum(item => item.FileSize),
                batchTransfer.TransferKind,
                batchTransfer.BatchId,
                null,
                FileTransferStatus.Rejected);
            foreach (var item in batchTransfer.Items)
            {
                await SaveOutgoingFileStatusAsync(item.FileId, FileTransferStatus.Rejected);
            }

            return;
        }

        if (batchTransfer.Items.Count == 0)
        {
            batchTransfer.State.MarkCompleted(response.FileId);
            batchTransfer.State.Apply();
            StatusMessage = $"{GetTransferKindText(batchTransfer.TransferKind)}已完成：{batchTransfer.Message.FileName}";
            await SaveBatchRecordAsync(
                batchTransfer.BatchId,
                batchTransfer.Receiver.UserId,
                batchTransfer.Message.FileName,
                0,
                batchTransfer.TransferKind,
                batchTransfer.BatchId,
                null,
                FileTransferStatus.Completed);
            return;
        }

        var resumeOffsets = (response.ResumeItems ?? [])
            .ToDictionary(item => item.FileId, item => item.Offset, StringComparer.Ordinal);
        batchTransfer.Message.StatusText = "正在批量传输";
        await SaveBatchRecordAsync(
            batchTransfer.BatchId,
            batchTransfer.Receiver.UserId,
            batchTransfer.Message.FileName,
            batchTransfer.Items.Sum(item => item.FileSize),
            batchTransfer.TransferKind,
            batchTransfer.BatchId,
            null,
            FileTransferStatus.Transferring);

        foreach (var item in batchTransfer.Items)
        {
            var resumeOffset = resumeOffsets.GetValueOrDefault(item.FileId);
            resumeOffset = Math.Clamp(resumeOffset, 0, item.FileSize);
            var progress = new Progress<double>(value =>
            {
                batchTransfer.State.MarkProgress(item.FileId, value);
                batchTransfer.State.Apply();
            });

            try
            {
                await SaveOutgoingFileStatusAsync(item.FileId, FileTransferStatus.Transferring, resumeOffset);
                await _fileTransferService.SendFileAsync(
                    batchTransfer.Receiver.IpAddress,
                    batchTransfer.Receiver.FilePort,
                    item.FileId,
                    item.SourcePath,
                    progress,
                    resumeOffset);
                batchTransfer.Message.StatusText = batchTransfer.State.BuildStatus();
            }
            catch (Exception ex)
            {
                _logger.Error("批量文件传输失败。", ex);
                batchTransfer.State.MarkFailed(item.FileId);
                batchTransfer.State.Apply();
                await SaveOutgoingFileStatusAsync(item.FileId, FileTransferStatus.Failed);
                StatusMessage = $"{GetTransferKindText(batchTransfer.TransferKind)}传输失败：{ex.Message}";

                try
                {
                    await _messageService.SendErrorAsync(
                        _settings,
                        batchTransfer.Receiver,
                        new ErrorPayload("FILE_TRANSFER_FAILED", $"文件传输失败：{item.FileName}", item.FileId));
                }
                catch (Exception notifyEx)
                {
                    _logger.Warning($"批量文件传输失败通知发送失败：{notifyEx.Message}");
                }
            }
        }

        StatusMessage = $"{GetTransferKindText(batchTransfer.TransferKind)}已发送，等待对方确认。";
    }

    private async Task HandleFileFinishedAsync(NetworkPacket packet)
    {
        var finished = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.FileTransferFinished);
        if (finished is null)
        {
            return;
        }

        if (_outgoingBatchStates.TryGetValue(finished.FileId, out var outgoingBatchState))
        {
            outgoingBatchState.MarkCompleted(finished.FileId);
            outgoingBatchState.Apply();
            await SaveOutgoingFileStatusAsync(finished.FileId, FileTransferStatus.Completed);
            StatusMessage = outgoingBatchState.IsComplete
                ? $"{GetTransferKindText(outgoingBatchState.TransferKind)}已全部接收。"
                : $"{GetTransferKindText(outgoingBatchState.TransferKind)}已有文件完成接收。";
            return;
        }

        if (_outgoingFileMessages.TryGetValue(finished.FileId, out var outgoingMessage))
        {
            if (_outgoingGroupFileStates.TryGetValue(finished.FileId, out var groupState))
            {
                groupState.MarkCompleted(finished.FileId);
                groupState.Apply();
                StatusMessage = $"群组附件已有成员确认接收：{outgoingMessage.FileName}";
            }
            else
            {
                outgoingMessage.Progress = 100;
                outgoingMessage.StatusText = "对方已接收";
                StatusMessage = $"对方已确认接收文件：{outgoingMessage.FileName}";
            }

            await SaveOutgoingFileStatusAsync(finished.FileId, FileTransferStatus.Completed);
            return;
        }

        if (_incomingFileMessages.TryGetValue(finished.FileId, out var incomingMessage))
        {
            incomingMessage.Progress = 100;
            incomingMessage.StatusText = "接收完成";
            await SaveIncomingFileStatusAsync(finished.FileId, FileTransferStatus.Completed);
            StatusMessage = $"文件接收完成：{incomingMessage.FileName}";
        }
    }

    private async Task HandleErrorAsync(NetworkPacket packet)
    {
        var error = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.ErrorPayload);
        if (error is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(error.FileId))
        {
            if (_outgoingBatchStates.TryGetValue(error.FileId, out var outgoingBatchState))
            {
                outgoingBatchState.MarkFailed(error.FileId);
                outgoingBatchState.Apply();
                await SaveOutgoingFileStatusAsync(error.FileId, FileTransferStatus.Failed);
            }

            if (_incomingBatchStates.TryGetValue(error.FileId, out var incomingBatchState))
            {
                incomingBatchState.MarkFailed(error.FileId);
                incomingBatchState.Apply();
                await SaveIncomingFileStatusAsync(error.FileId, FileTransferStatus.Failed);
            }

            if (_outgoingFileMessages.TryGetValue(error.FileId, out var outgoingMessage))
            {
                outgoingMessage.StatusText = "传输失败";
                if (_outgoingGroupFileStates.TryGetValue(error.FileId, out var groupState))
                {
                    groupState.MarkFailed(error.FileId);
                    groupState.Apply();
                }

                await SaveOutgoingFileStatusAsync(error.FileId, FileTransferStatus.Failed);
            }

            if (_incomingFileMessages.TryGetValue(error.FileId, out var incomingMessage))
            {
                incomingMessage.StatusText = "传输失败";
                await SaveIncomingFileStatusAsync(error.FileId, FileTransferStatus.Failed);
            }
        }

        StatusMessage = $"收到来自 {ResolveUserName(packet.FromUserId)} 的错误：{error.Message}";
    }

    private async Task HandleReadReceiptAsync(NetworkPacket packet)
    {
        var receipt = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.MessageReadReceiptPayload);
        if (receipt is null)
        {
            return;
        }

        var result = await _chatHistoryService.MarkMessageReadByAsync(receipt);
        var message = Messages.FirstOrDefault(item => item.MessageId == receipt.MessageId);
        if (message is not null)
        {
            message.ReadByCount = result.ReadByCount;
            message.ReadTargetCount = result.ReadTargetCount;
            message.IsRead = result.IsRead;
        }

        StatusMessage = receipt.IsGroup
            ? $"{receipt.ReaderNickname} 已读群组消息。"
            : $"{receipt.ReaderNickname} 已读你的消息。";
    }

    private async Task HandleMessageRecallAsync(NetworkPacket packet)
    {
        var recall = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.MessageRecallPayload);
        if (recall is null)
        {
            return;
        }

        await _chatHistoryService.RecallMessageAsync(recall.SessionId, recall.MessageId, recall.RecallTime);
        if (Messages.FirstOrDefault(item => item.MessageId == recall.MessageId) is { } message)
        {
            MarkMessageRecalled(message, isMine: false);
        }

        var session = recall.IsGroup
            ? RecentSessions.FirstOrDefault(item => item.UserId == recall.SessionId)
            : EnsureUserSession(packet.FromUserId, recall.SenderNickname, "0.0.0.0");
        if (session is not null)
        {
            UpsertRecentSession(session, $"{recall.SenderNickname} 撤回了一条消息", moveToTop: true);
            if (SelectedUser?.UserId != session.UserId)
            {
                session.UnreadCount++;
                RefreshUnreadState();
            }
        }

        StatusMessage = $"{recall.SenderNickname} 撤回了一条消息。";
    }

    private Task HandleOfflineFileReminderAsync(NetworkPacket packet)
    {
        var reminder = JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.OfflineFileReminderPayload);
        if (reminder is null)
        {
            return Task.CompletedTask;
        }

        var session = !string.IsNullOrWhiteSpace(reminder.GroupId)
            ? EnsureGroupSession(new ChatGroup
            {
                GroupId = reminder.GroupId!,
                Name = string.IsNullOrWhiteSpace(reminder.GroupName) ? "未命名群组" : reminder.GroupName!,
                Kind = reminder.GroupKind,
                MemberUserIds = [reminder.SenderId, reminder.ReceiverId, _settings.UserId],
                CreatedTime = DateTimeOffset.Now,
                UpdatedTime = DateTimeOffset.Now
            })
            : EnsureUserSession(reminder.SenderId, reminder.SenderNickname, "0.0.0.0");
        var transferText = reminder.TransferKind == FileTransferKind.Folder
            ? "文件夹"
            : reminder.TransferKind == FileTransferKind.MultipleFiles ? "多文件" : reminder.IsImage ? "图片" : "文件";
        var content = !string.IsNullOrWhiteSpace(reminder.GroupId)
            ? $"{reminder.SenderNickname} 有一个离线{transferText}等待发送：{reminder.FileName}"
            : $"{reminder.SenderNickname} 有一个离线{transferText}等待发送：{reminder.FileName}";
        var fileMessage = new ChatMessageViewModel
        {
            MessageId = reminder.ReminderId,
            SessionId = session.UserId,
            SenderId = reminder.SenderId,
            ReceiverId = reminder.ReceiverId,
            SenderName = reminder.SenderNickname,
            Content = content,
            TimeText = reminder.CreatedTime.ToString("HH:mm"),
            Kind = MessageKind.File,
            FileName = reminder.FileName,
            FileSizeText = FormatFileSize(reminder.FileSize),
            Progress = 0,
            StatusText = "对方上线后会重新发送请求"
        };

        if (SelectedUser?.UserId == session.UserId)
        {
            Messages.Add(fileMessage);
        }
        else
        {
            session.UnreadCount++;
            RefreshUnreadState();
        }

        UpsertRecentSession(session, $"[离线{transferText}提醒] {reminder.FileName}", moveToTop: true);
        StatusMessage = $"收到离线{transferText}提醒：{reminder.FileName}";
        RequestNotification($"离线{transferText}提醒", content, session.UserId);
        return Task.CompletedTask;
    }

    private OnlineUserViewModel EnsureBroadcastSession()
    {
        if (_broadcastSession is null)
        {
            _broadcastSession = new OnlineUserViewModel
            {
                UserId = NetworkConstants.BroadcastSessionId,
                Nickname = "全员广播",
                IpAddress = "所有在线用户",
                Status = UserStatus.Online,
                LastMessage = "向局域网所有在线用户发送消息"
            };
        }

        UpsertRecentSession(_broadcastSession, moveToTop: false);
        return _broadcastSession;
    }

    private OnlineUserViewModel EnsureUserSession(string userId, string nickname, string ipAddress)
    {
        var existing = OnlineUsers.FirstOrDefault(user => user.UserId == userId);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(nickname) && existing.Nickname != nickname)
            {
                existing.Nickname = nickname;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress) && ipAddress != "0.0.0.0" && existing.IpAddress != ipAddress)
            {
                existing.IpAddress = ipAddress;
            }

            RefreshFilteredUsers();
            return existing;
        }

        var created = new OnlineUserViewModel
        {
            UserId = userId,
            Nickname = string.IsNullOrWhiteSpace(nickname) ? userId : nickname,
            Department = NetworkConstants.DefaultDepartment,
            IpAddress = ipAddress,
            Status = UserStatus.Offline,
            LastMessage = "已记录为已知联系人"
        };

        OnlineUsers.Add(created);
        OnPropertyChanged(nameof(OnlineCount));
        RefreshFilteredUsers();
        return created;
    }

    private OnlineUserViewModel EnsureGroupSession(ChatGroup group)
    {
        var existing = RecentSessions.FirstOrDefault(session => session.UserId == group.GroupId);
        if (existing is null)
        {
            existing = new OnlineUserViewModel
            {
                UserId = group.GroupId,
                Nickname = string.IsNullOrWhiteSpace(group.Name) ? "未命名群组" : group.Name,
                Department = "群组",
                IpAddress = "多人会话",
                Status = UserStatus.Online,
                IsGroupSession = true,
                GroupKind = group.Kind,
                LastActiveTime = group.UpdatedTime,
                LastMessage = $"{(group.Kind == GroupKind.Permanent ? "永久群组" : "临时群组")} · {group.MemberUserIds.Count} 人"
            };
            RecentSessions.Add(existing);
        }
        else
        {
            existing.Nickname = string.IsNullOrWhiteSpace(group.Name) ? existing.Nickname : group.Name;
            existing.Department = "群组";
            existing.IpAddress = "多人会话";
            existing.IsGroupSession = true;
            existing.GroupKind = group.Kind;
            existing.Status = UserStatus.Online;
            existing.LastActiveTime = group.UpdatedTime > existing.LastActiveTime ? group.UpdatedTime : existing.LastActiveTime;
        }

        existing.GroupMemberIds.Clear();
        existing.GroupMemberIds.AddRange(group.MemberUserIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal));
        existing.RefreshGroupMetadata();
        RefreshFilteredUsers();
        return existing;
    }

    private OnlineUserViewModel UpsertOnlineUser(UserInfo user)
    {
        var existing = OnlineUsers.FirstOrDefault(item => item.UserId == user.UserId);
        if (existing is null)
        {
            existing = OnlineUserViewModel.FromUser(user);
            OnlineUsers.Add(existing);
            RefreshFilteredUsers();
            return existing;
        }

        existing.Nickname = user.Nickname;
        existing.Department = user.Department;
        existing.IpAddress = user.IpAddress;
        existing.MessagePort = user.MessagePort;
        existing.FilePort = user.FilePort;
        existing.Status = user.Status;

        if (string.IsNullOrWhiteSpace(existing.LastMessage) ||
            existing.LastMessage is "等待重新上线" or "已离线" or "可以开始聊天" or "已记录为已知联系人")
        {
            existing.LastMessage = user.Status == UserStatus.Online ? "可以开始聊天" : "已离线";
        }

        RefreshFilteredUsers();
        return existing;
    }

    private void UpsertRecentSession(OnlineUserViewModel session, string? lastMessage = null, bool moveToTop = true)
    {
        if (!string.IsNullOrWhiteSpace(lastMessage))
        {
            session.LastMessage = lastMessage;
        }

        if (moveToTop)
        {
            session.LastActiveTime = DateTimeOffset.Now;
        }

        var existing = RecentSessions.FirstOrDefault(item => item.UserId == session.UserId);
        if (existing is not null)
        {
            if (moveToTop)
            {
                ReorderRecentSessions();
            }

            RefreshFilteredUsers();
            return;
        }

        RecentSessions.Add(session);
        if (moveToTop)
        {
            ReorderRecentSessions();
        }

        RefreshFilteredUsers();
    }

    private async Task LoadKnownUsersAsync()
    {
        var knownUsers = await _userRepository.LoadRecentAsync(20);
        foreach (var knownUser in knownUsers.Where(user => user.UserId != _settings.UserId))
        {
            var offlineUser = new UserInfo
            {
                UserId = knownUser.UserId,
                Nickname = knownUser.Nickname,
                Department = knownUser.Department,
                IpAddress = knownUser.IpAddress,
                MessagePort = knownUser.MessagePort,
                FilePort = knownUser.FilePort,
                Status = UserStatus.Offline,
                LastSeenTime = knownUser.LastSeenTime
            };

            var user = UpsertOnlineUser(offlineUser);
            UpsertRecentSession(user, user.LastMessage, moveToTop: false);
        }

        OnPropertyChanged(nameof(OnlineCount));
        RefreshFilteredUsers();
    }

    private async Task LoadPermanentGroupsAsync()
    {
        var groups = await _groupRepository.LoadAllAsync();
        foreach (var group in groups)
        {
            var session = EnsureGroupSession(group);
            UpsertRecentSession(session, session.LastMessage, moveToTop: false);
        }

        RefreshFilteredUsers();
    }

    private void RefreshFilteredUsers()
    {
        var query = SearchText.Trim();
        ReplaceCollection(FilteredRecentSessions, RecentSessions.Where(user => MatchesSearch(user, query)));

        var filteredUsers = OnlineUsers
            .Where(user => MatchesSearch(user, query))
            .OrderBy(user => user.DepartmentText, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(user => user.Status == UserStatus.Online)
            .ThenBy(user => user.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ReplaceCollection(FilteredOnlineUsers, filteredUsers);
        ReplaceCollection(
            ContactGroups,
            filteredUsers
                .GroupBy(user => user.DepartmentText, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ContactGroupViewModel(
                    group.Key,
                    group
                        .OrderByDescending(user => user.Status == UserStatus.Online)
                        .ThenBy(user => user.Nickname, StringComparer.OrdinalIgnoreCase))));
    }

    private static bool MatchesSearch(OnlineUserViewModel user, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return ContainsIgnoreCase(user.Nickname, query) ||
            ContainsIgnoreCase(user.DepartmentText, query) ||
            ContainsIgnoreCase(user.IpAddress, query) ||
            ContainsIgnoreCase(user.LastMessage, query) ||
            ContainsIgnoreCase(user.StatusText, query);
    }

    private static bool ContainsIgnoreCase(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsMessageSearchActive()
    {
        return !string.IsNullOrWhiteSpace(MessageSearchText);
    }

    private void RefreshUnreadState()
    {
        TotalUnreadCount = RecentSessions.Sum(session => session.UnreadCount);
    }

    private async Task MarkSessionMessagesReadAsync(OnlineUserViewModel session)
    {
        if (session.UserId == NetworkConstants.BroadcastSessionId)
        {
            return;
        }

        try
        {
            var readTime = DateTimeOffset.Now;
            var messages = await _chatHistoryService.MarkSessionIncomingMessagesReadAsync(session.UserId, readTime);
            foreach (var message in messages)
            {
                await SendReadReceiptForMessageAsync(message, session.IsGroupSession, readTime);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"发送已读回执失败：{ex.Message}");
        }
    }

    private async Task MarkIncomingMessageReadAsync(ChatMessage message, bool isGroup)
    {
        try
        {
            var readTime = DateTimeOffset.Now;
            await _chatHistoryService.MarkSessionIncomingMessagesReadAsync(message.SessionId, readTime);
            await SendReadReceiptForMessageAsync(message, isGroup, readTime);
        }
        catch (Exception ex)
        {
            _logger.Warning($"发送已读回执失败：{ex.Message}");
        }
    }

    private async Task SendReadReceiptForMessageAsync(ChatMessage message, bool isGroup, DateTimeOffset readTime)
    {
        var receipt = new MessageReadReceiptPayload(
            message.MessageId,
            message.SessionId,
            _settings.UserId,
            LocalNickname,
            isGroup,
            readTime);

        if (isGroup)
        {
            var group = RecentSessions.FirstOrDefault(session => session.UserId == message.SessionId);
            if (group is null)
            {
                return;
            }

            var sender = ResolveKnownUserInfo(message.SenderId);
            if (sender.Status == UserStatus.Online)
            {
                await _messageService.SendReadReceiptAsync(_settings, sender, receipt);
                return;
            }

            await QueueControlDeliveryAsync(sender.UserId, PacketType.MessageReadReceipt, message.MessageId, JsonSerializer.Serialize(receipt, LanTalkJsonContext.Default.MessageReadReceiptPayload), "消息发送者离线，已读回执等待补发。");
            return;
        }

        var receiver = ResolveKnownUserInfo(message.SenderId);
        if (receiver.Status == UserStatus.Online)
        {
            await _messageService.SendReadReceiptAsync(_settings, receiver, receipt);
            return;
        }

        await QueueControlDeliveryAsync(receiver.UserId, PacketType.MessageReadReceipt, message.MessageId, JsonSerializer.Serialize(receipt, LanTalkJsonContext.Default.MessageReadReceiptPayload), "对方离线，已读回执等待补发。");
    }

    private async Task SendRecallToRecipientAsync(string recipientId, MessageRecallPayload payload)
    {
        var receiver = ResolveKnownUserInfo(recipientId);
        var payloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.MessageRecallPayload);
        if (receiver.Status == UserStatus.Online)
        {
            try
            {
                await _messageService.SendMessageRecallAsync(_settings, receiver, payload);
                return;
            }
            catch (Exception ex)
            {
                _logger.Warning($"撤回通知发送给 {receiver.Nickname}({receiver.IpAddress}) 失败，已加入补发队列：{ex.Message}");
                await QueueControlDeliveryAsync(recipientId, PacketType.MessageRecall, payload.MessageId, payloadJson, ex.Message);
                return;
            }
        }

        await QueueControlDeliveryAsync(recipientId, PacketType.MessageRecall, payload.MessageId, payloadJson, "对方离线，撤回通知等待补发。");
    }

    private static void MarkMessageRecalled(ChatMessageViewModel message, bool isMine)
    {
        message.IsRecalled = true;
        message.Content = isMine ? "你撤回了一条消息" : "对方撤回了一条消息";
        message.FileName = string.Empty;
        message.FileSizeText = string.Empty;
        message.Progress = 0;
        message.StatusText = string.Empty;
    }

    private int GetReadTargetCount(OnlineUserViewModel session, MessageKind kind)
    {
        if (kind == MessageKind.Group)
        {
            return session.GroupMemberIds
                .Where(id => !string.IsNullOrWhiteSpace(id) && id != _settings.UserId)
                .Distinct(StringComparer.Ordinal)
                .Count();
        }

        return kind == MessageKind.Private ? 1 : 0;
    }

    private IEnumerable<UserInfo> ResolveGroupReceivers(OnlineUserViewModel group)
    {
        var memberIds = group.GroupMemberIds.ToHashSet(StringComparer.Ordinal);
        return OnlineUsers
            .Where(user => memberIds.Contains(user.UserId) && user.UserId != _settings.UserId)
            .Select(ToUserInfo)
            .ToArray();
    }

    private UserInfo ResolveKnownUserInfo(string userId)
    {
        var user = OnlineUsers.FirstOrDefault(item => item.UserId == userId);
        if (user is not null)
        {
            return ToUserInfo(user);
        }

        return new UserInfo
        {
            UserId = userId,
            Nickname = userId,
            Department = NetworkConstants.DefaultDepartment,
            IpAddress = string.Empty,
            MessagePort = _settings.MessagePort,
            FilePort = _settings.FilePort,
            Status = UserStatus.Offline,
            LastSeenTime = DateTimeOffset.Now
        };
    }

    private async Task<GroupQueuedSendResult> SendGroupMessageWithOfflineQueueAsync(
        OnlineUserViewModel group,
        GroupMessagePayload payload,
        CancellationToken cancellationToken = default)
    {
        var memberIds = group.GroupMemberIds
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != _settings.UserId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var onlineReceivers = ResolveGroupReceivers(group)
            .Where(user => user.Status == UserStatus.Online)
            .ToDictionary(user => user.UserId, StringComparer.Ordinal);
        var payloadJson = JsonSerializer.Serialize(payload, LanTalkJsonContext.Default.GroupMessagePayload);
        var requiresEncryption = _encryptedGroupSessions.Contains(group.UserId);
        var success = 0;
        var queued = 0;
        var failure = 0;

        foreach (var memberId in memberIds)
        {
            if (!onlineReceivers.TryGetValue(memberId, out var receiver))
            {
                await QueueGroupMessageDeliveryAsync(
                    memberId,
                    payload,
                    payloadJson,
                    requiresEncryption,
                    requiresEncryption ? "成员当前离线，等待重新上线并完成加密协商后补发。" : "成员当前离线，等待重新上线后补发。",
                    cancellationToken);
                queued++;
                continue;
            }

            if (requiresEncryption && !_messageService.GetEncryptionState(receiver.UserId).IsEnabled)
            {
                await QueueEncryptedGroupMessageUntilReadyAsync(receiver, payload, payloadJson, cancellationToken);
                queued++;
                continue;
            }

            try
            {
                await _messageService.SendGroupMessageToAsync(_settings, receiver, payload, requiresEncryption, cancellationToken);
                success++;
            }
            catch (Exception ex)
            {
                _logger.Warning($"群组消息发送给 {receiver.Nickname}({receiver.IpAddress}) 失败，已加入离线补发队列：{ex.Message}");
                await QueueGroupMessageDeliveryAsync(memberId, payload, payloadJson, requiresEncryption, ex.Message, cancellationToken);
                queued++;
            }
        }

        if (memberIds.Length == 0)
        {
            failure = 1;
        }

        return new GroupQueuedSendResult(success, queued, failure, requiresEncryption);
    }

    private async Task QueueEncryptedGroupMessageUntilReadyAsync(
        UserInfo receiver,
        GroupMessagePayload payload,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var state = _messageService.GetEncryptionState(receiver.UserId);
        if (!state.IsPending)
        {
            try
            {
                await _messageService.EnableEncryptionAsync(_settings, receiver, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Warning($"向群组成员 {receiver.Nickname} 发起端到端加密协商失败：{ex.Message}");
            }
        }

        await QueueGroupMessageDeliveryAsync(
            receiver.UserId,
            payload,
            payloadJson,
            requiresEncryption: true,
            "等待与该成员完成端到端加密协商后补发。",
            cancellationToken);
    }

    private Task QueueGroupMessageDeliveryAsync(
        string recipientId,
        GroupMessagePayload payload,
        string payloadJson,
        bool requiresEncryption,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return _outgoingDeliveryRepository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = CreateDeliveryId(PacketType.GroupMessage, recipientId, payload.MessageId),
            RecipientId = recipientId,
            PacketType = PacketType.GroupMessage,
            PayloadJson = payloadJson,
            RequiresEncryption = requiresEncryption,
            CreatedTime = DateTimeOffset.Now,
            LastError = reason
        }, cancellationToken);
    }

    private Task QueueGroupFileDeliveryAsync(
        string recipientId,
        FileTransferRequest request,
        string sourcePath,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return QueueFileDeliveryAsync(recipientId, request, sourcePath, reason, cancellationToken);
    }

    private Task QueueGroupBatchDeliveryAsync(
        string recipientId,
        FileTransferRequest request,
        string sourceMap,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return QueueFileDeliveryAsync(recipientId, request, sourceMap, reason, cancellationToken);
    }

    private Task QueueFileDeliveryAsync(
        string recipientId,
        FileTransferRequest request,
        string sourcePath,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(request, LanTalkJsonContext.Default.FileTransferRequest);
        return _outgoingDeliveryRepository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = CreateDeliveryId(PacketType.FileRequest, recipientId, request.FileId),
            RecipientId = recipientId,
            PacketType = PacketType.FileRequest,
            PayloadJson = payloadJson,
            SourcePath = sourcePath,
            CreatedTime = DateTimeOffset.Now,
            LastError = reason
        }, cancellationToken);
    }

    private Task QueueControlDeliveryAsync(
        string recipientId,
        PacketType packetType,
        string messageId,
        string payloadJson,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return _outgoingDeliveryRepository.SaveAsync(new OutgoingDeliveryRecord
        {
            DeliveryId = CreateDeliveryId(packetType, recipientId, messageId),
            RecipientId = recipientId,
            PacketType = packetType,
            PayloadJson = payloadJson,
            CreatedTime = DateTimeOffset.Now,
            LastError = reason
        }, cancellationToken);
    }

    private async Task RetryPendingDeliveriesAsync(OnlineUserViewModel user)
    {
        if (user.Status != UserStatus.Online || user.UserId == _settings.UserId)
        {
            return;
        }

        lock (_retryingDeliveryRecipients)
        {
            if (!_retryingDeliveryRecipients.Add(user.UserId))
            {
                return;
            }
        }

        try
        {
            var receiver = ToUserInfo(user);
            var records = await _outgoingDeliveryRepository.LoadForRecipientAsync(user.UserId);
            foreach (var record in records)
            {
                if (record.PacketType == PacketType.GroupMessage)
                {
                    await RetryGroupMessageDeliveryAsync(receiver, record);
                    continue;
                }

                if (record.PacketType == PacketType.FileRequest)
                {
                    await RetryGroupFileDeliveryAsync(receiver, record);
                    continue;
                }

                if (record.PacketType == PacketType.MessageReadReceipt)
                {
                    await RetryReadReceiptDeliveryAsync(receiver, record);
                    continue;
                }

                if (record.PacketType == PacketType.MessageRecall)
                {
                    await RetryMessageRecallDeliveryAsync(receiver, record);
                    continue;
                }
            }
        }
        finally
        {
            lock (_retryingDeliveryRecipients)
            {
                _retryingDeliveryRecipients.Remove(user.UserId);
            }
        }
    }

    private async Task RetryGroupMessageDeliveryAsync(UserInfo receiver, OutgoingDeliveryRecord record)
    {
        var payload = JsonSerializer.Deserialize(record.PayloadJson, LanTalkJsonContext.Default.GroupMessagePayload);
        if (payload is null)
        {
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            return;
        }

        if (record.RequiresEncryption && !_messageService.GetEncryptionState(receiver.UserId).IsEnabled)
        {
            var state = _messageService.GetEncryptionState(receiver.UserId);
            if (!state.IsPending)
            {
                try
                {
                    await _messageService.EnableEncryptionAsync(_settings, receiver);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"补发加密群组消息前发起加密协商失败：{ex.Message}");
                }
            }

            var reason = "等待端到端加密协商完成后补发。";
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, reason);
            StatusMessage = $"{receiver.Nickname} 的群组加密消息已保留在补发队列。";
            return;
        }

        try
        {
            await _messageService.SendGroupMessageToAsync(_settings, receiver, payload, record.RequiresEncryption);
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            StatusMessage = record.RequiresEncryption
                ? $"已向 {receiver.Nickname} 加密补发群组“{payload.GroupName}”的离线消息。"
                : $"已向 {receiver.Nickname} 补发群组“{payload.GroupName}”的离线消息。";
        }
        catch (Exception ex)
        {
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, ex.Message);
            _logger.Warning($"离线群组消息补发给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
        }
    }

    private async Task RetryReadReceiptDeliveryAsync(UserInfo receiver, OutgoingDeliveryRecord record)
    {
        var payload = JsonSerializer.Deserialize(record.PayloadJson, LanTalkJsonContext.Default.MessageReadReceiptPayload);
        if (payload is null)
        {
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            return;
        }

        try
        {
            await _messageService.SendReadReceiptAsync(_settings, receiver, payload);
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
        }
        catch (Exception ex)
        {
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, ex.Message);
            _logger.Warning($"已读回执补发给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
        }
    }

    private async Task RetryMessageRecallDeliveryAsync(UserInfo receiver, OutgoingDeliveryRecord record)
    {
        var payload = JsonSerializer.Deserialize(record.PayloadJson, LanTalkJsonContext.Default.MessageRecallPayload);
        if (payload is null)
        {
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            return;
        }

        try
        {
            await _messageService.SendMessageRecallAsync(_settings, receiver, payload);
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
        }
        catch (Exception ex)
        {
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, ex.Message);
            _logger.Warning($"撤回通知补发给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
        }
    }

    private async Task RetryGroupFileDeliveryAsync(UserInfo receiver, OutgoingDeliveryRecord record)
    {
        var request = JsonSerializer.Deserialize(record.PayloadJson, LanTalkJsonContext.Default.FileTransferRequest);
        if (request is null)
        {
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            return;
        }

        if (request.IsBatchTransfer)
        {
            await RetryGroupBatchFileDeliveryAsync(receiver, record, request);
            return;
        }

        if (string.IsNullOrWhiteSpace(record.SourcePath) || !File.Exists(record.SourcePath))
        {
            var reason = $"源文件不可用，无法补发：{request.FileName}";
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, reason);
            StatusMessage = reason;
            _logger.Warning(reason);
            return;
        }

        var message = EnsureOutgoingFileMessageForRetry(request, record.SourcePath);
        _outgoingFilePaths[request.FileId] = record.SourcePath;
        _outgoingFileReceivers[request.FileId] = receiver;
        _outgoingFileMessages[request.FileId] = message;

        try
        {
            await SendOfflineFileReminderForRequestAsync(receiver, request);
            await _messageService.SendFileRequestAsync(_settings, receiver, request);
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            message.StatusText = "离线补发请求已发送";
            StatusMessage = $"已向 {receiver.Nickname} 补发群组附件请求：{request.FileName}";
        }
        catch (Exception ex)
        {
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, ex.Message);
            message.StatusText = "离线补发失败";
            _logger.Warning($"离线群组附件补发给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
        }
    }

    private async Task RetryGroupBatchFileDeliveryAsync(UserInfo receiver, OutgoingDeliveryRecord record, FileTransferRequest request)
    {
        if (string.IsNullOrWhiteSpace(record.SourcePath))
        {
            var reason = $"批量附件源路径映射不可用，无法补发：{request.FileName}";
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, reason);
            StatusMessage = reason;
            _logger.Warning(reason);
            return;
        }

        var sourceMap = ParseSourceMap(record.SourcePath);
        var fileItems = request.TransferItems.Where(item => !item.IsDirectory).ToArray();
        var missingItem = fileItems.FirstOrDefault(item => !sourceMap.TryGetValue(item.FileId, out var path) || !File.Exists(path));
        if (missingItem is not null)
        {
            var reason = $"源文件不可用，无法补发：{missingItem.FileName}";
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, reason);
            StatusMessage = reason;
            _logger.Warning(reason);
            return;
        }

        var totalSize = fileItems.Sum(item => item.FileSize);
        var message = CreateBatchMessage(request.FileName, request.TransferKind, fileItems.Length, totalSize, isMine: true);
        message.StatusText = "等待离线补发";
        var state = new FileBatchTransferState(message, fileItems.Length, totalSize, request.TransferKind, isSender: true);
        var outgoingItems = new List<OutgoingBatchItem>();

        foreach (var item in fileItems)
        {
            var sourcePath = sourceMap[item.FileId];
            var localItem = new LocalTransferItem(item.FileId, sourcePath, item.FileName, item.RelativePath, item.FileSize, IsDirectory: false);
            RegisterOutgoingBatchItem(localItem, receiver, message, state);
            outgoingItems.Add(new OutgoingBatchItem(item.FileId, sourcePath, item.FileName, item.RelativePath, item.FileSize));
        }

        _outgoingFileMessages[request.FileId] = message;
        _outgoingFileReceivers[request.FileId] = receiver;
        _outgoingBatchTransfers[request.FileId] = new OutgoingBatchTransfer(request.FileId, receiver, outgoingItems, message, state, request.TransferKind);

        if (!string.IsNullOrWhiteSpace(request.GroupId) && SelectedUser?.UserId == request.GroupId)
        {
            Messages.Add(message);
        }

        try
        {
            await SendOfflineFileReminderForRequestAsync(receiver, request);
            await _messageService.SendFileRequestAsync(_settings, receiver, request);
            await _outgoingDeliveryRepository.DeleteAsync(record.DeliveryId);
            message.StatusText = "离线补发请求已发送";
            StatusMessage = $"已向 {receiver.Nickname} 补发群组{GetTransferKindText(request.TransferKind)}请求：{request.FileName}";
        }
        catch (Exception ex)
        {
            await _outgoingDeliveryRepository.MarkAttemptAsync(record.DeliveryId, record.AttemptCount + 1, ex.Message);
            message.StatusText = "离线补发失败";
            _logger.Warning($"离线群组批量附件补发给 {receiver.Nickname}({receiver.IpAddress}) 失败：{ex.Message}");
        }
    }

    private async Task SendOfflineFileReminderForRequestAsync(UserInfo receiver, FileTransferRequest request)
    {
        try
        {
            var reminder = new OfflineFileReminderPayload(
                Guid.NewGuid().ToString("N"),
                request.FileId,
                request.FileName,
                request.FileSize,
                _settings.UserId,
                LocalNickname,
                receiver.UserId,
                request.IsImage,
                request.TransferKind,
                request.BatchId,
                request.BatchName,
                request.GroupId,
                request.GroupName,
                request.GroupKind,
                DateTimeOffset.Now);
            await _messageService.SendOfflineFileReminderAsync(_settings, receiver, reminder);
        }
        catch (Exception ex)
        {
            _logger.Warning($"离线文件提醒发送失败，将继续发送文件请求：{ex.Message}");
        }
    }

    private ChatMessageViewModel EnsureOutgoingFileMessageForRetry(FileTransferRequest request, string sourcePath)
    {
        if (_outgoingFileMessages.TryGetValue(request.FileId, out var existing))
        {
            return existing;
        }

        var message = new ChatMessageViewModel
        {
            MessageId = request.FileId,
            SessionId = request.GroupId ?? request.ReceiverId,
            SenderId = _settings.UserId,
            ReceiverId = request.GroupId ?? request.ReceiverId,
            SenderName = LocalNickname,
            Content = request.IsImage ? string.Empty : $"群组文件：{request.FileName}",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            IsMine = true,
            Kind = request.IsImage ? MessageKind.Image : MessageKind.File,
            FileName = request.FileName,
            FileSizeText = FormatFileSize(request.FileSize),
            Progress = 0,
            StatusText = "等待离线补发"
        };

        if (request.IsImage)
        {
            message.SetImagePath(sourcePath);
        }

        if (!string.IsNullOrWhiteSpace(request.GroupId) && SelectedUser?.UserId == request.GroupId)
        {
            Messages.Add(message);
        }

        return message;
    }

    private static string CreateDeliveryId(PacketType packetType, string recipientId, string messageId)
    {
        return $"{packetType}:{recipientId}:{messageId}";
    }

    private void ReorderRecentSessions()
    {
        var ordered = OrderRecentSessions(RecentSessions).ToArray();
        for (var targetIndex = 0; targetIndex < ordered.Length; targetIndex++)
        {
            var currentIndex = RecentSessions.IndexOf(ordered[targetIndex]);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                RecentSessions.Move(currentIndex, targetIndex);
            }
        }
    }

    private static IEnumerable<OnlineUserViewModel> OrderRecentSessions(IEnumerable<OnlineUserViewModel> sessions)
    {
        return sessions
            .OrderByDescending(session => session.UnreadCount > 0)
            .ThenByDescending(session => session.LastActiveTime)
            .ThenBy(session => session.Nickname, StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSelectionState(OnlineUserViewModel selected)
    {
        foreach (var user in OnlineUsers)
        {
            user.IsSelected = user.UserId == selected.UserId;
        }

        foreach (var session in RecentSessions)
        {
            session.IsSelected = session.UserId == selected.UserId;
        }
    }

    private void SelectRelativeRecentSession(int offset)
    {
        var sessions = FilteredRecentSessions.Count > 0 ? FilteredRecentSessions : RecentSessions;
        if (sessions.Count == 0)
        {
            StatusMessage = "暂无可切换的会话。";
            return;
        }

        var currentIndex = FindSessionIndex(sessions, SelectedUser?.UserId);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + offset + sessions.Count) % sessions.Count;
        SelectedUser = sessions[nextIndex];
    }

    private static int FindSessionIndex(IReadOnlyList<OnlineUserViewModel> sessions, string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return -1;
        }

        for (var i = 0; i < sessions.Count; i++)
        {
            if (sessions[i].UserId == userId)
            {
                return i;
            }
        }

        return -1;
    }

    private static string NormalizeDepartment(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? NetworkConstants.DefaultDepartment
            : trimmed;
    }

    private void RequestNotification(string title, string message, string sessionId)
    {
        UserNotificationRequested?.Invoke(this, new UserNotificationEventArgs(title, message, sessionId));
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private async Task PersistKnownUsersAsync(IEnumerable<UserInfo> users)
    {
        try
        {
            await _userRepository.SaveManyAsync(users.Where(user => user.UserId != _settings.UserId));
        }
        catch (Exception ex)
        {
            _logger.Error("保存已知用户失败。", ex);
        }
    }

    private async Task LoadSessionMessagesAsync(OnlineUserViewModel user)
    {
        if (!string.IsNullOrWhiteSpace(MessageSearchText))
        {
            await SearchSessionMessagesAsync(user, MessageSearchText);
            return;
        }

        await LoadRecentSessionMessagesAsync(user);
    }

    private async Task LoadRecentSessionMessagesAsync(OnlineUserViewModel user, CancellationToken cancellationToken = default)
    {
        Messages.Clear();
        MessageSearchStatus = string.Empty;

        if (user.UserId == NetworkConstants.BroadcastSessionId)
        {
            var broadcastHistory = await _chatHistoryService.LoadRecentMessagesAsync(NetworkConstants.BroadcastSessionId, cancellationToken);
            foreach (var message in broadcastHistory)
            {
                Messages.Add(ToMessageViewModel(message, message.IsMine ? LocalNickname : "局域网广播"));
            }

            if (Messages.Count == 0)
            {
                Messages.Add(new ChatMessageViewModel
                {
                    SenderName = "局域网广播",
                    Content = "这里用于向当前在线的局域网用户发送广播消息。",
                    TimeText = DateTimeOffset.Now.ToString("HH:mm"),
                    Kind = MessageKind.Broadcast
                });
            }

            return;
        }

        if (user.IsGroupSession)
        {
            var groupHistory = await _chatHistoryService.LoadRecentMessagesAsync(user.UserId, cancellationToken);
            foreach (var message in groupHistory)
            {
                Messages.Add(ToMessageViewModel(message, message.IsMine ? LocalNickname : ResolveUserName(message.SenderId)));
            }

            if (Messages.Count == 0)
            {
                Messages.Add(new ChatMessageViewModel
                {
                    SenderName = user.Nickname,
                    Content = $"这里是“{user.Nickname}”多人会话，消息会点对点发送给当前在线成员。",
                    TimeText = DateTimeOffset.Now.ToString("HH:mm"),
                    Kind = MessageKind.System
                });
            }

            return;
        }

        var history = await _chatHistoryService.LoadRecentMessagesAsync(user.UserId, cancellationToken);
        foreach (var message in history)
        {
            Messages.Add(ToMessageViewModel(message, message.IsMine ? LocalNickname : user.Nickname));
        }

        if (Messages.Count > 0)
        {
            return;
        }

        Messages.Add(new ChatMessageViewModel
        {
            SenderName = user.Nickname,
            Content = $"还没有和 {user.Nickname} 的聊天记录，可以直接发送第一条消息。",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            Kind = MessageKind.Private
        });
    }

    private async Task ApplyMessageSearchAsync(string query)
    {
        if (SelectedUser is null)
        {
            MessageSearchStatus = string.Empty;
            return;
        }

        _messageSearchCts?.Cancel();
        _messageSearchCts?.Dispose();
        _messageSearchCts = new CancellationTokenSource();
        var cancellationToken = _messageSearchCts.Token;

        try
        {
            await Task.Delay(250, cancellationToken);

            if (string.IsNullOrWhiteSpace(query))
            {
                await LoadRecentSessionMessagesAsync(SelectedUser, cancellationToken);
                return;
            }

            await SearchSessionMessagesAsync(SelectedUser, query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.Error("聊天记录搜索失败。", ex);
            MessageSearchStatus = "搜索失败";
            StatusMessage = $"聊天记录搜索失败：{ex.Message}";
        }
    }

    private async Task SearchSessionMessagesAsync(OnlineUserViewModel user, string query, CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            await LoadRecentSessionMessagesAsync(user, cancellationToken);
            return;
        }

        var sessionId = user.UserId == NetworkConstants.BroadcastSessionId
            ? NetworkConstants.BroadcastSessionId
            : user.UserId;

        var results = await _chatHistoryService.SearchMessagesAsync(sessionId, trimmedQuery, cancellationToken);
        Messages.Clear();

        foreach (var message in results)
        {
            var senderName = message.IsMine
                ? LocalNickname
                : user.UserId == NetworkConstants.BroadcastSessionId
                    ? "局域网广播"
                    : user.IsGroupSession
                        ? ResolveUserName(message.SenderId)
                    : user.Nickname;
            Messages.Add(ToMessageViewModel(message, senderName));
        }

        MessageSearchStatus = results.Count == 0
            ? $"没有找到包含“{trimmedQuery}”的消息"
            : $"找到 {results.Count} 条包含“{trimmedQuery}”的消息";

        if (results.Count == 0)
        {
            Messages.Add(new ChatMessageViewModel
            {
                SenderName = "搜索",
                Content = "当前会话没有匹配的聊天记录。",
                TimeText = DateTimeOffset.Now.ToString("HH:mm"),
                Kind = MessageKind.System
            });
        }
    }

    private void StartFileServer()
    {
        if (_fileServerCts is not null)
        {
            return;
        }

        _fileServerCts = new CancellationTokenSource();
        _fileServerTask = Task.Run(() => _fileServer.StartAsync(
            _settings.FilePort,
            CreateReceiveFileStreamAsync,
            UpdateReceiveProgressAsync,
            _fileServerCts.Token));
    }

    private async Task<Stream> CreateReceiveFileStreamAsync(string fileId, long fileSize, long resumeOffset, CancellationToken cancellationToken)
    {
        var request = _pendingFileRequests.GetValueOrDefault(fileId);
        var fileName = request?.FileName ?? $"{fileId}.bin";
        var savePath = _incomingSavePaths.GetValueOrDefault(fileId) ?? Path.Combine(_settings.FileSavePath, fileName);
        _incomingSavePaths[fileId] = savePath;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? _settings.FileSavePath);
            var stream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            if (resumeOffset == 0)
            {
                stream.SetLength(0);
            }
            else
            {
                if (stream.Length < resumeOffset)
                {
                    stream.Dispose();
                    throw new IOException("本地未完成文件小于续传偏移量，无法继续接收。");
                }

                stream.SetLength(resumeOffset);
                stream.Seek(resumeOffset, SeekOrigin.Begin);
            }

            if (_incomingFileMessages.TryGetValue(fileId, out var message))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    message.StatusText = resumeOffset > 0 ? "正在断点续传" : "正在接收";
                    message.Progress = fileSize == 0 ? 100 : resumeOffset * 100d / fileSize;
                });
            }

            return stream;
        }
        catch (Exception ex) when (IsFileSaveException(ex))
        {
            _logger.Error("文件保存失败。", ex);
            await MarkIncomingFileFailedAsync(fileId, $"文件保存失败：{ex.Message}", cancellationToken);
            throw;
        }
    }

    private async Task UpdateReceiveProgressAsync(string fileId, double progress, CancellationToken cancellationToken)
    {
        var batchState = _incomingBatchStates.GetValueOrDefault(fileId);
        if (_incomingFileMessages.TryGetValue(fileId, out var message))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (batchState is not null)
                {
                    batchState.MarkProgress(fileId, progress);
                    if (progress >= 100)
                    {
                        batchState.MarkCompleted(fileId);
                    }

                    batchState.Apply();
                }
                else
                {
                    message.Progress = progress;
                    message.StatusText = progress >= 100 ? "接收完成" : $"正在接收 {progress:0}%";
                }

                if (progress >= 100 && _incomingSavePaths.TryGetValue(fileId, out var savePath))
                {
                    message.SetImagePath(savePath);
                }
            });
        }

        if (progress >= 100 && _pendingFileRequests.TryGetValue(fileId, out var request))
        {
            await SaveIncomingFileStatusAsync(fileId, FileTransferStatus.Completed, cancellationToken);

            UserInfo? sender = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                sender = OnlineUsers.FirstOrDefault(user => user.UserId == request.SenderId) is { } user
                    ? ToUserInfo(user)
                    : null;
                StatusMessage = $"文件接收完成：{request.FileName}";
            });

            if (sender is not null && _incomingFinishedNotifications.Add(fileId))
            {
                try
                {
                    await _messageService.SendFileFinishedAsync(_settings, sender, fileId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"文件完成确认发送失败：{ex.Message}");
                }
            }
        }
    }

    private async Task MarkIncomingFileFailedAsync(string fileId, string statusText, CancellationToken cancellationToken)
    {
        UserInfo? sender = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_incomingFileMessages.TryGetValue(fileId, out var message))
            {
                message.Progress = 0;
                message.StatusText = "保存失败";
            }

            if (_pendingFileRequests.TryGetValue(fileId, out var request))
            {
                sender = OnlineUsers.FirstOrDefault(user => user.UserId == request.SenderId) is { } user
                    ? ToUserInfo(user)
                    : null;
            }

            StatusMessage = statusText;
        });

        await SaveIncomingFileStatusAsync(fileId, FileTransferStatus.Failed, cancellationToken);

        if (sender is null)
        {
            return;
        }

        try
        {
            await _messageService.SendErrorAsync(
                _settings,
                sender,
                new ErrorPayload("FILE_SAVE_FAILED", statusText, fileId),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Warning($"文件保存失败通知发送失败：{ex.Message}");
        }
    }

    private static bool IsFileSaveException(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException or NotSupportedException or ArgumentException;
    }

    private async Task SaveIncomingFileStatusAsync(string fileId, FileTransferStatus status, CancellationToken cancellationToken = default)
    {
        if (!_pendingFileRequests.TryGetValue(fileId, out var request))
        {
            return;
        }

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SavePath = _incomingSavePaths.GetValueOrDefault(fileId),
            TransferKind = request.TransferKind,
            BatchId = request.BatchId,
            RelativePath = request.RelativePath,
            BytesTransferred = status == FileTransferStatus.Completed
                ? request.FileSize
                : GetExistingFileLength(_incomingSavePaths.GetValueOrDefault(fileId)),
            Status = status,
            TransferTime = DateTimeOffset.Now
        }, cancellationToken);
    }

    private async Task SaveOutgoingFileStatusAsync(string fileId, FileTransferStatus status, long? bytesTransferred = null)
    {
        if (!_outgoingFilePaths.TryGetValue(fileId, out var path) ||
            !_outgoingFileReceivers.TryGetValue(fileId, out var receiver) ||
            !_outgoingFileMessages.TryGetValue(fileId, out var message))
        {
            return;
        }

        var fileInfo = new FileInfo(path);
        var batchInfo = FindOutgoingBatchInfo(fileId);
        var fileSize = batchInfo?.FileSize ?? (fileInfo.Exists ? fileInfo.Length : 0);
        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = fileId,
            SenderId = _settings.UserId,
            ReceiverId = receiver.UserId,
            FileName = batchInfo?.FileName ?? message.FileName,
            FileSize = fileSize,
            SavePath = path,
            TransferKind = batchInfo?.TransferKind ?? FileTransferKind.SingleFile,
            BatchId = batchInfo?.BatchId,
            RelativePath = batchInfo?.RelativePath,
            BytesTransferred = bytesTransferred ?? (status == FileTransferStatus.Completed ? fileSize : 0),
            Status = status,
            TransferTime = DateTimeOffset.Now
        });
    }

    private async Task SaveBatchRecordAsync(
        string fileId,
        string receiverId,
        string fileName,
        long fileSize,
        FileTransferKind transferKind,
        string? batchId,
        string? relativePath,
        FileTransferStatus status,
        string? savePath = null,
        long bytesTransferred = 0)
    {
        await SaveBatchRecordAsync(
            fileId,
            _settings.UserId,
            receiverId,
            fileName,
            fileSize,
            transferKind,
            batchId,
            relativePath,
            status,
            savePath,
            bytesTransferred);
    }

    private Task SaveBatchRecordAsync(
        string fileId,
        string senderId,
        string receiverId,
        string fileName,
        long fileSize,
        FileTransferKind transferKind,
        string? batchId,
        string? relativePath,
        FileTransferStatus status,
        string? savePath = null,
        long bytesTransferred = 0)
    {
        return _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = fileId,
            SenderId = senderId,
            ReceiverId = receiverId,
            FileName = fileName,
            FileSize = fileSize,
            SavePath = savePath,
            TransferKind = transferKind,
            BatchId = batchId,
            RelativePath = relativePath,
            BytesTransferred = bytesTransferred,
            Status = status,
            TransferTime = DateTimeOffset.Now
        });
    }

    private static ChatMessageViewModel ToMessageViewModel(ChatMessage message, string senderName)
    {
        var viewModel = new ChatMessageViewModel
        {
            MessageId = message.MessageId,
            SessionId = message.SessionId,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            SenderName = senderName,
            Content = message.IsRecalled
                ? message.IsMine ? "你撤回了一条消息" : "对方撤回了一条消息"
                : message.Content,
            TimeText = message.SendTime.ToString("HH:mm"),
            IsMine = message.IsMine,
            Kind = message.Kind,
            IsRead = message.IsRead,
            ReadByCount = message.ReadByCount,
            ReadTargetCount = message.ReadTargetCount,
            IsRecalled = message.IsRecalled
        };

        if (message.Kind != MessageKind.Image || message.IsRecalled)
        {
            return viewModel;
        }

        var image = DeserializeImageMessageContent(message.Content);
        viewModel.Content = string.Empty;
        viewModel.FileName = image?.FileName ?? "图片";
        viewModel.FileSizeText = image is null ? string.Empty : FormatFileSize(image.FileSize);
        viewModel.StatusText = image is not null && !string.IsNullOrWhiteSpace(image.LocalPath) && File.Exists(image.LocalPath)
            ? "可预览"
            : "图片文件不可用";
        viewModel.SetImagePath(image?.LocalPath);
        return viewModel;
    }

    private string ResolveUserName(string userId)
    {
        return OnlineUsers.FirstOrDefault(user => user.UserId == userId)?.Nickname ?? userId;
    }

    private static UserInfo ToUserInfo(OnlineUserViewModel user)
    {
        return new UserInfo
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            Department = user.DepartmentText,
            IpAddress = user.IpAddress,
            MessagePort = user.MessagePort,
            FilePort = user.FilePort,
            Status = user.Status,
            LastSeenTime = DateTimeOffset.Now
        };
    }

    private static AppServices CreateDefaultServices()
    {
        var logger = new ConsoleLanTalkLogger();
        var settingsService = new SettingsService(logger);
        var connectionFactory = new SqliteConnectionFactory();
        var historyService = new ChatHistoryService(new MessageRepository(connectionFactory));
        var registry = new OnlineUserRegistry();
        var discoveryServer = new UdpDiscoveryServer(logger);
        var discoveryService = new DiscoveryService(registry, discoveryServer, logger);
        var messageService = new MessageService(new TcpMessageClient(), new TcpMessageServer(logger), logger);
        var fileTransferRepository = new FileTransferRepository(connectionFactory);
        var userRepository = new UserRepository(connectionFactory);
        var groupRepository = new GroupRepository(connectionFactory);
        var outgoingDeliveryRepository = new OutgoingDeliveryRepository(connectionFactory);
        var fileTransferService = new FileTransferService();
        var fileServer = new TcpFileServer(logger);

        return new AppServices(settingsService, historyService, discoveryService, messageService, fileTransferService, fileTransferRepository, userRepository, groupRepository, outgoingDeliveryRepository, fileServer, logger);
    }

    private static async Task<IReadOnlyList<string>> PickFilesAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            return [];
        }

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要发送的文件",
            AllowMultiple = true
        });

        return files
            .Select(file => file.Path.LocalPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static async Task<string?> PickFolderAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            return null;
        }

        var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择要发送的文件夹",
            AllowMultiple = false
        });

        return folders.Count == 0 ? null : folders[0].Path.LocalPath;
    }

    private static async Task<string?> PickImageAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            return null;
        }

        var imageType = new FilePickerFileType("图片文件")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"],
            MimeTypes = ["image/png", "image/jpeg", "image/gif", "image/bmp", "image/webp"]
        };

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要发送的图片",
            AllowMultiple = false,
            FileTypeFilter = [imageType, FilePickerFileTypes.All]
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    private ChatMessage CreateImageChatMessage(
        string fileId,
        string peerUserId,
        FileInfo fileInfo,
        string localPath,
        bool isMine,
        string? sessionId = null,
        string? senderId = null,
        string? receiverId = null,
        string? fileName = null,
        long? fileSize = null,
        int readTargetCount = 0)
    {
        return new ChatMessage
        {
            MessageId = fileId,
            SessionId = sessionId ?? peerUserId,
            SenderId = senderId ?? (isMine ? _settings.UserId : peerUserId),
            ReceiverId = receiverId ?? (isMine ? peerUserId : _settings.UserId),
            Kind = MessageKind.Image,
            Content = SerializeImageMessageContent(new ImageMessageContent(
                fileId,
                fileName ?? fileInfo.Name,
                fileSize ?? fileInfo.Length,
                localPath)),
            SendTime = DateTimeOffset.Now,
            IsMine = isMine,
            ReadTargetCount = readTargetCount
        };
    }

    private static string SerializeImageMessageContent(ImageMessageContent content)
    {
        return JsonSerializer.Serialize(content, LanTalkJsonContext.Default.ImageMessageContent);
    }

    private static ImageMessageContent? DeserializeImageMessageContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(content, LanTalkJsonContext.Default.ImageMessageContent);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsImageFileName(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdatePreviewImageSize()
    {
        if (_previewImagePixelWidth <= 0 || _previewImagePixelHeight <= 0)
        {
            PreviewImageDisplayWidth = 0;
            PreviewImageDisplayHeight = 0;
            return;
        }

        var baseScale = Math.Min(920d / _previewImagePixelWidth, 560d / _previewImagePixelHeight);
        baseScale = Math.Min(1, baseScale);
        var scale = baseScale * PreviewImageZoom;
        PreviewImageDisplayWidth = Math.Max(120, _previewImagePixelWidth * scale);
        PreviewImageDisplayHeight = Math.Max(90, _previewImagePixelHeight * scale);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private static IReadOnlyList<LocalTransferItem> CreateLocalTransferItems(
        IReadOnlyList<FileInfo> files,
        FileTransferKind transferKind,
        string? rootFolderPath,
        IReadOnlyList<DirectoryInfo>? directories)
    {
        var items = new List<LocalTransferItem>();
        if (transferKind == FileTransferKind.Folder && !string.IsNullOrWhiteSpace(rootFolderPath))
        {
            foreach (var directory in directories ?? [])
            {
                var relativePath = NormalizeOutgoingRelativePath(Path.GetRelativePath(rootFolderPath, directory.FullName));
                if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
                {
                    continue;
                }

                items.Add(new LocalTransferItem(
                    Guid.NewGuid().ToString("N"),
                    directory.FullName,
                    directory.Name,
                    relativePath,
                    0,
                    IsDirectory: true));
            }

            foreach (var file in files.Where(file => file.Exists))
            {
                var relativePath = NormalizeOutgoingRelativePath(Path.GetRelativePath(rootFolderPath, file.FullName));
                items.Add(new LocalTransferItem(
                    Guid.NewGuid().ToString("N"),
                    file.FullName,
                    file.Name,
                    relativePath,
                    file.Length,
                    IsDirectory: false));
            }

            return items;
        }

        var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(file => file.Exists))
        {
            var relativePath = CreateUniqueFileName(file.Name, usedNames);
            items.Add(new LocalTransferItem(
                Guid.NewGuid().ToString("N"),
                file.FullName,
                file.Name,
                relativePath,
                file.Length,
                IsDirectory: false));
        }

        return items;
    }

    private static LocalTransferItem[] CloneTransferItemsForRecipient(IReadOnlyList<LocalTransferItem> localItems)
    {
        return localItems
            .Select(item => item with { FileId = Guid.NewGuid().ToString("N") })
            .ToArray();
    }

    private void RegisterOutgoingBatchItem(
        LocalTransferItem item,
        UserInfo receiver,
        ChatMessageViewModel message,
        FileBatchTransferState state)
    {
        _outgoingFilePaths[item.FileId] = item.SourcePath;
        _outgoingFileReceivers[item.FileId] = receiver;
        _outgoingFileMessages[item.FileId] = message;
        _outgoingBatchStates[item.FileId] = state;
    }

    private ChatMessageViewModel CreateBatchMessage(
        string displayName,
        FileTransferKind transferKind,
        int fileCount,
        long totalSize,
        bool isMine,
        string? senderName = null)
    {
        return new ChatMessageViewModel
        {
            SenderName = senderName ?? (isMine ? LocalNickname : "对方"),
            Content = transferKind == FileTransferKind.Folder ? $"文件夹：{displayName}" : $"多文件：{displayName}",
            TimeText = DateTimeOffset.Now.ToString("HH:mm"),
            IsMine = isMine,
            Kind = MessageKind.File,
            FileName = displayName,
            FileSizeText = BuildBatchSizeText(fileCount, totalSize),
            Progress = 0,
            StatusText = "等待确认"
        };
    }

    private static string BuildBatchDisplayName(FileTransferKind transferKind, string batchName, int fileCount)
    {
        return transferKind switch
        {
            FileTransferKind.Folder => string.IsNullOrWhiteSpace(batchName) ? "文件夹" : batchName,
            FileTransferKind.MultipleFiles => $"{fileCount} 个文件",
            _ => batchName
        };
    }

    private static string BuildBatchRecentText(FileTransferKind transferKind, string displayName)
    {
        return transferKind == FileTransferKind.Folder ? $"[文件夹] {displayName}" : $"[多文件] {displayName}";
    }

    private static string BuildIncomingBatchRecentText(FileTransferKind transferKind, string displayName)
    {
        return transferKind == FileTransferKind.Folder ? $"收到文件夹：{displayName}" : $"收到多文件：{displayName}";
    }

    private static string GetTransferKindText(FileTransferKind transferKind)
    {
        return transferKind switch
        {
            FileTransferKind.Folder => "文件夹",
            FileTransferKind.MultipleFiles => "多文件",
            _ => "文件"
        };
    }

    private static string BuildBatchSizeText(int fileCount, long totalSize)
    {
        return $"{fileCount} 个文件 · {FormatFileSize(totalSize)}";
    }

    private string BuildIncomingBatchRootPath(FileTransferRequest request, string displayName)
    {
        var safeName = ToSafePathSegment(displayName);
        var suffix = request.FileId.Length >= 8 ? request.FileId[..8] : request.FileId;
        return Path.Combine(_settings.FileSavePath, $"{safeName}-{suffix}");
    }

    private static string NormalizeOutgoingRelativePath(string relativePath)
    {
        return string.Join(
            Path.DirectorySeparatorChar,
            relativePath
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CreateUniqueFileName(string fileName, Dictionary<string, int> usedNames)
    {
        var safeName = ToSafePathSegment(fileName);
        if (!usedNames.TryGetValue(safeName, out var count))
        {
            usedNames[safeName] = 1;
            return safeName;
        }

        count++;
        usedNames[safeName] = count;
        var extension = Path.GetExtension(safeName);
        var stem = Path.GetFileNameWithoutExtension(safeName);
        return $"{stem} ({count}){extension}";
    }

    private static string ToSafePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "传输批次" : safe;
    }

    private static bool TryNormalizeTransferRelativePath(
        string relativePath,
        bool isDirectory,
        out string normalizedPath,
        out string? error)
    {
        normalizedPath = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            error = "路径为空";
            return false;
        }

        var candidate = relativePath.Trim();
        if (Path.IsPathRooted(candidate))
        {
            error = "不能使用绝对路径";
            return false;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var parts = candidate
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var safeParts = new List<string>();
        foreach (var part in parts)
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                error = "不能包含上级目录";
                return false;
            }

            if (part.IndexOfAny(invalid) >= 0)
            {
                error = $"路径片段包含非法字符：{part}";
                return false;
            }

            safeParts.Add(part);
        }

        if (safeParts.Count == 0)
        {
            if (isDirectory)
            {
                normalizedPath = ".";
                return true;
            }

            error = "文件名为空";
            return false;
        }

        normalizedPath = Path.Combine(safeParts.ToArray());
        return true;
    }

    private static long GetResumeOffset(string savePath, long fileSize)
    {
        if (!File.Exists(savePath))
        {
            return 0;
        }

        var length = new FileInfo(savePath).Length;
        if (length < 0 || length > fileSize)
        {
            return 0;
        }

        return length;
    }

    private static long GetExistingFileLength(string? savePath)
    {
        return !string.IsNullOrWhiteSpace(savePath) && File.Exists(savePath)
            ? new FileInfo(savePath).Length
            : 0;
    }

    private OutgoingBatchInfo? FindOutgoingBatchInfo(string fileId)
    {
        foreach (var transfer in _outgoingBatchTransfers.Values)
        {
            var item = transfer.Items.FirstOrDefault(batchItem => batchItem.FileId == fileId);
            if (item is not null)
            {
                return new OutgoingBatchInfo(
                    transfer.BatchId,
                    transfer.TransferKind,
                    item.FileName,
                    item.RelativePath,
                    item.FileSize);
            }
        }

        return null;
    }

    private static string BuildSourceMap(IReadOnlyList<LocalTransferItem> localItems)
    {
        return string.Join(
            '\n',
            localItems
                .Where(item => !item.IsDirectory)
                .Select(item => $"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.FileId))}\t{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.SourcePath))}"));
    }

    private static IReadOnlyDictionary<string, string> ParseSourceMap(string sourceMap)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in sourceMap.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length != 2)
            {
                continue;
            }

            try
            {
                var fileId = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                var sourcePath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                result[fileId] = sourcePath;
            }
            catch (FormatException)
            {
            }
        }

        return result;
    }

    private sealed record LocalTransferItem(
        string FileId,
        string SourcePath,
        string FileName,
        string RelativePath,
        long FileSize,
        bool IsDirectory)
    {
        public FileTransferItem ToTransferItem()
        {
            return new FileTransferItem(FileId, FileName, RelativePath, FileSize, IsDirectory);
        }
    }

    private sealed record OutgoingBatchItem(
        string FileId,
        string SourcePath,
        string FileName,
        string RelativePath,
        long FileSize);

    private sealed record OutgoingBatchTransfer(
        string BatchId,
        UserInfo Receiver,
        IReadOnlyList<OutgoingBatchItem> Items,
        ChatMessageViewModel Message,
        FileBatchTransferState State,
        FileTransferKind TransferKind);

    private sealed record OutgoingBatchInfo(
        string BatchId,
        FileTransferKind TransferKind,
        string FileName,
        string RelativePath,
        long FileSize);

    private sealed record GroupQueuedSendResult(int SuccessCount, int QueuedCount, int FailureCount, bool IsEncrypted);

    private sealed class FileBatchTransferState
    {
        private readonly ChatMessageViewModel message;
        private readonly Dictionary<string, double> progressByFileId = new(StringComparer.Ordinal);
        private readonly HashSet<string> completedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> rejectedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> failedFileIds = new(StringComparer.Ordinal);

        public FileBatchTransferState(ChatMessageViewModel message, int totalFileCount, long totalBytes, FileTransferKind transferKind, bool isSender)
        {
            this.message = message;
            TotalFileCount = totalFileCount;
            TotalBytes = totalBytes;
            TransferKind = transferKind;
            IsSender = isSender;
        }

        public int TotalFileCount { get; }

        public long TotalBytes { get; }

        public FileTransferKind TransferKind { get; }

        public bool IsSender { get; }

        public bool IsComplete => TotalFileCount == 0 || completedFileIds.Count >= TotalFileCount;

        public void MarkProgress(string fileId, double progress)
        {
            progressByFileId[fileId] = Math.Clamp(progress, 0, 100);
        }

        public void MarkCompleted(string fileId)
        {
            progressByFileId[fileId] = 100;
            completedFileIds.Add(fileId);
            failedFileIds.Remove(fileId);
        }

        public void MarkRejected(string fileId)
        {
            rejectedFileIds.Add(fileId);
        }

        public void MarkFailed(string fileId)
        {
            failedFileIds.Add(fileId);
        }

        public string BuildStatus()
        {
            if (TotalFileCount == 0)
            {
                return IsSender ? "空文件夹已发送" : "空文件夹已接收";
            }

            var verb = IsSender ? "发送" : "接收";
            var completed = completedFileIds.Count;
            var failed = failedFileIds.Count;
            var rejected = rejectedFileIds.Count;
            if (completed + failed + rejected >= TotalFileCount)
            {
                return failed == 0 && rejected == 0
                    ? $"{verb}完成 · {completed}/{TotalFileCount}"
                    : $"{verb}结束 · 完成 {completed}/{TotalFileCount}，拒绝 {rejected}，失败 {failed}";
            }

            return $"正在{verb} {message.Progress:0}% · 完成 {completed}/{TotalFileCount}，拒绝 {rejected}，失败 {failed}";
        }

        public void Apply()
        {
            message.Progress = TotalFileCount == 0
                ? 100
                : progressByFileId.Values.DefaultIfEmpty(0).Sum() / TotalFileCount;
            message.StatusText = BuildStatus();
        }
    }

    private sealed class GroupFileTransferState
    {
        private readonly HashSet<string> requestSentFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> queuedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> acceptedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> rejectedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> completedFileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> failedFileIds = new(StringComparer.Ordinal);

        public GroupFileTransferState(ChatMessageViewModel message, int totalCount)
        {
            Message = message;
            TotalCount = totalCount;
        }

        private ChatMessageViewModel Message { get; }

        private int TotalCount { get; }

        public void MarkRequestSent(string fileId)
        {
            requestSentFileIds.Add(fileId);
            queuedFileIds.Remove(fileId);
        }

        public void MarkQueued(string fileId)
        {
            queuedFileIds.Add(fileId);
        }

        public void MarkAccepted(string fileId)
        {
            acceptedFileIds.Add(fileId);
        }

        public void MarkRejected(string fileId)
        {
            rejectedFileIds.Add(fileId);
        }

        public void MarkCompleted(string fileId)
        {
            completedFileIds.Add(fileId);
            failedFileIds.Remove(fileId);
        }

        public void MarkFailed(string fileId)
        {
            failedFileIds.Add(fileId);
        }

        public string BuildStatus(double? activeProgress = null)
        {
            var completed = completedFileIds.Count;
            var rejected = rejectedFileIds.Count;
            var failed = failedFileIds.Count;
            var queued = queuedFileIds.Count;
            var sent = requestSentFileIds.Count;

            if (activeProgress is not null)
            {
                return $"群发中 {activeProgress:0}% · 完成 {completed}/{TotalCount}，待补发 {queued}，拒绝 {rejected}，失败 {failed}";
            }

            if (TotalCount > 0 && completed + rejected + failed >= TotalCount)
            {
                return failed == 0 && rejected == 0
                    ? $"群发完成 · {completed}/{TotalCount} 已接收"
                    : $"群发结束 · 完成 {completed}/{TotalCount}，待补发 {queued}，拒绝 {rejected}，失败 {failed}";
            }

            if (acceptedFileIds.Count > 0)
            {
                return $"群发进行中 · 完成 {completed}/{TotalCount}，已接受 {acceptedFileIds.Count}，待补发 {queued}，拒绝 {rejected}，失败 {failed}";
            }

            return $"群发请求 {sent}/{TotalCount} · 待补发 {queued}，拒绝 {rejected}，失败 {failed}";
        }

        public void Apply()
        {
            Message.Progress = TotalCount == 0 ? 0 : completedFileIds.Count * 100d / TotalCount;
            Message.StatusText = BuildStatus();
        }
    }
}

public sealed record AppServices(
    SettingsService SettingsService,
    ChatHistoryService ChatHistoryService,
    DiscoveryService DiscoveryService,
    MessageService MessageService,
    FileTransferService FileTransferService,
    FileTransferRepository FileTransferRepository,
    UserRepository UserRepository,
    GroupRepository GroupRepository,
    OutgoingDeliveryRepository OutgoingDeliveryRepository,
    TcpFileServer FileServer,
    ILanTalkLogger Logger);
