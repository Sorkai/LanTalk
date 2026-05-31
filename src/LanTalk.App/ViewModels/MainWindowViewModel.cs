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
    private readonly TcpFileServer _fileServer;
    private readonly ILanTalkLogger _logger;
    private readonly Dictionary<string, FileTransferRequest> _pendingFileRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _outgoingFilePaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UserInfo> _outgoingFileReceivers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatMessageViewModel> _outgoingFileMessages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatMessageViewModel> _incomingFileMessages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _incomingSavePaths = new(StringComparer.Ordinal);
    private readonly HashSet<string> _incomingFinishedNotifications = new(StringComparer.Ordinal);
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
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId || SelectedUser.IsGroupSession)
        {
            RefreshConversationEncryptionState();
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

    private void OnEncryptionStateChanged(object? sender, ConversationEncryptionStateChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SelectedUser?.UserId == e.State.UserId)
            {
                RefreshConversationEncryptionState();
                UpdateCurrentSessionHeader();
            }

            StatusMessage = e.State.StatusText;
        });
    }

    private void OnEncryptionError(object? sender, EncryptionErrorEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SelectedUser?.UserId == e.PeerUserId)
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
            ApplyConversationEncryptionState(false, false, "群组暂不支持端到端加密", "群组消息会点对点发送给当前在线成员。");
            return;
        }

        var state = _messageService.GetEncryptionState(SelectedUser.UserId);
        var tip = string.IsNullOrWhiteSpace(state.Fingerprint)
            ? "开启后会使用临时 ECDH 密钥协商与 AES-GCM 加密私聊文本。"
            : $"请与对方比对加密指纹：{state.Fingerprint}";
        ApplyConversationEncryptionState(true, state.IsEnabled || state.IsPending, state.StatusText, tip);
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
            IsMine = true
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
                var result = await _messageService.SendGroupMessageAsync(_settings, ResolveGroupReceivers(SelectedUser), payload);
                StatusMessage = result.FailureCount == 0
                    ? $"群组消息已发送给 {result.SuccessCount} 个在线成员。"
                    : $"群组消息已发送 {result.SuccessCount} 人，失败 {result.FailureCount} 人。";
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

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = request.FileId,
            SenderId = request.SenderId,
            ReceiverId = request.ReceiverId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SavePath = savePath,
            Status = FileTransferStatus.Accepted,
            TransferTime = DateTimeOffset.Now
        });

        await _messageService.SendFileResponseAsync(_settings, ToUserInfo(sender), new FileTransferResponse(request.FileId, true));
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
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId || SelectedUser.IsGroupSession)
        {
            StatusMessage = "请先选择一个在线用户再发送文件；群组文件发送会在后续支持。";
            return;
        }

        var selectedPath = await PickFileAsync();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        await SendSelectedFileAsync(selectedPath, isImage: false);
    }

    [RelayCommand]
    private async Task AttachImageAsync()
    {
        if (SelectedUser is null || SelectedUser.UserId == NetworkConstants.BroadcastSessionId || SelectedUser.IsGroupSession)
        {
            StatusMessage = "请先选择一个在线用户再发送图片；群组图片发送会在后续支持。";
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

        var fileId = Guid.NewGuid().ToString("N");
        var receiver = ToUserInfo(SelectedUser);
        var request = new FileTransferRequest(fileId, fileInfo.Name, fileInfo.Length, _settings.UserId, receiver.UserId, _settings.FilePort, isImage);
        var fileMessage = new ChatMessageViewModel
        {
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
            await _chatHistoryService.SaveMessageAsync(CreateImageChatMessage(fileId, receiver.UserId, fileInfo, selectedPath, isMine: true));
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
            fileMessage.StatusText = "发送请求失败";
            StatusMessage = isImage ? $"图片请求发送失败：{ex.Message}" : $"文件请求发送失败：{ex.Message}";
        }
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

        var isImage = request.IsImage || IsImageFileName(request.FileName);
        _pendingFileRequests[request.FileId] = request;
        var sender = ResolveUserName(packet.FromUserId);
        var senderSession = EnsureUserSession(packet.FromUserId, sender, "0.0.0.0");
        var savePath = Path.Combine(_settings.FileSavePath, request.FileName);

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
                fileName: request.FileName,
                fileSize: request.FileSize));
        }

        var fileMessage = new ChatMessageViewModel
        {
            SenderName = sender,
            Content = isImage ? string.Empty : "收到文件发送请求，请确认是否接收。",
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

        if (SelectedUser?.UserId == request.SenderId)
        {
            Messages.Add(fileMessage);
        }
        else
        {
            senderSession.UnreadCount++;
            RefreshUnreadState();
        }

        UpsertRecentSession(senderSession, isImage ? $"[图片] {request.FileName}" : $"收到文件：{request.FileName}", moveToTop: true);
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
            IsImage = isImage
        };
        IsFileRequestPaneOpen = true;
        StatusMessage = isImage
            ? $"收到 {sender} 的图片：{request.FileName}"
            : $"收到 {sender} 的文件请求：{request.FileName}";
        RequestNotification(
            isImage ? "收到图片" : "收到文件请求",
            isImage ? $"{sender} 发来图片：{request.FileName}" : $"{sender} 想发送：{request.FileName}",
            request.SenderId);
    }

    private async Task HandleFileResponseAsync(NetworkPacket packet)
    {
        var response = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.FileTransferResponse);
        if (response is null)
        {
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
            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Rejected);
            return;
        }

        if (!_outgoingFilePaths.TryGetValue(response.FileId, out var path) ||
            !_outgoingFileReceivers.TryGetValue(response.FileId, out var receiver))
        {
            fileMessage.StatusText = "文件路径丢失";
            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Failed);
            return;
        }

        fileMessage.StatusText = "正在传输";
        await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Transferring);

        try
        {
            var progress = new Progress<double>(value =>
            {
                fileMessage.Progress = value;
                fileMessage.StatusText = $"正在传输 {value:0}%";
            });

            await _fileTransferService.SendFileAsync(receiver.IpAddress, receiver.FilePort, response.FileId, path, progress);
            fileMessage.Progress = 100;
            fileMessage.StatusText = "已发送，等待对方确认";
            StatusMessage = $"文件已发送，等待对方确认：{fileMessage.FileName}";
            await SaveOutgoingFileStatusAsync(response.FileId, FileTransferStatus.Transferring);
        }
        catch (Exception ex)
        {
            _logger.Error("文件传输失败。", ex);
            fileMessage.StatusText = "传输失败";
            StatusMessage = $"文件传输失败：{ex.Message}";
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

    private async Task HandleFileFinishedAsync(NetworkPacket packet)
    {
        var finished = System.Text.Json.JsonSerializer.Deserialize(packet.PayloadJson, LanTalkJsonContext.Default.FileTransferFinished);
        if (finished is null)
        {
            return;
        }

        if (_outgoingFileMessages.TryGetValue(finished.FileId, out var outgoingMessage))
        {
            outgoingMessage.Progress = 100;
            outgoingMessage.StatusText = "对方已接收";
            await SaveOutgoingFileStatusAsync(finished.FileId, FileTransferStatus.Completed);
            StatusMessage = $"对方已确认接收文件：{outgoingMessage.FileName}";
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
            if (_outgoingFileMessages.TryGetValue(error.FileId, out var outgoingMessage))
            {
                outgoingMessage.StatusText = "传输失败";
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

    private IEnumerable<UserInfo> ResolveGroupReceivers(OnlineUserViewModel group)
    {
        var memberIds = group.GroupMemberIds.ToHashSet(StringComparer.Ordinal);
        return OnlineUsers
            .Where(user => memberIds.Contains(user.UserId) && user.UserId != _settings.UserId)
            .Select(ToUserInfo)
            .ToArray();
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

    private async Task<Stream> CreateReceiveFileStreamAsync(string fileId, long fileSize, CancellationToken cancellationToken)
    {
        var request = _pendingFileRequests.GetValueOrDefault(fileId);
        var fileName = request?.FileName ?? $"{fileId}.bin";
        var savePath = _incomingSavePaths.GetValueOrDefault(fileId) ?? Path.Combine(_settings.FileSavePath, fileName);
        _incomingSavePaths[fileId] = savePath;

        try
        {
            Directory.CreateDirectory(_settings.FileSavePath);
            var stream = File.Create(savePath);

            if (_incomingFileMessages.TryGetValue(fileId, out var message))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    message.StatusText = "正在接收";
                    message.Progress = 0;
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
        if (_incomingFileMessages.TryGetValue(fileId, out var message))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                message.Progress = progress;
                message.StatusText = progress >= 100 ? "接收完成" : $"正在接收 {progress:0}%";
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
            Status = status,
            TransferTime = DateTimeOffset.Now
        }, cancellationToken);
    }

    private async Task SaveOutgoingFileStatusAsync(string fileId, FileTransferStatus status)
    {
        if (!_outgoingFilePaths.TryGetValue(fileId, out var path) ||
            !_outgoingFileReceivers.TryGetValue(fileId, out var receiver) ||
            !_outgoingFileMessages.TryGetValue(fileId, out var message))
        {
            return;
        }

        await _fileTransferRepository.SaveAsync(new FileTransferRecord
        {
            FileId = fileId,
            SenderId = _settings.UserId,
            ReceiverId = receiver.UserId,
            FileName = message.FileName,
            FileSize = new FileInfo(path).Exists ? new FileInfo(path).Length : 0,
            SavePath = path,
            Status = status,
            TransferTime = DateTimeOffset.Now
        });
    }

    private static ChatMessageViewModel ToMessageViewModel(ChatMessage message, string senderName)
    {
        var viewModel = new ChatMessageViewModel
        {
            SenderName = senderName,
            Content = message.Content,
            TimeText = message.SendTime.ToString("HH:mm"),
            IsMine = message.IsMine,
            Kind = message.Kind
        };

        if (message.Kind != MessageKind.Image)
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
        var fileTransferService = new FileTransferService();
        var fileServer = new TcpFileServer(logger);

        return new AppServices(settingsService, historyService, discoveryService, messageService, fileTransferService, fileTransferRepository, userRepository, groupRepository, fileServer, logger);
    }

    private static async Task<string?> PickFileAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            return null;
        }

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要发送的文件",
            AllowMultiple = false
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
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
        string? fileName = null,
        long? fileSize = null)
    {
        return new ChatMessage
        {
            MessageId = fileId,
            SessionId = peerUserId,
            SenderId = isMine ? _settings.UserId : peerUserId,
            ReceiverId = isMine ? peerUserId : _settings.UserId,
            Kind = MessageKind.Image,
            Content = SerializeImageMessageContent(new ImageMessageContent(
                fileId,
                fileName ?? fileInfo.Name,
                fileSize ?? fileInfo.Length,
                localPath)),
            SendTime = DateTimeOffset.Now,
            IsMine = isMine
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
    TcpFileServer FileServer,
    ILanTalkLogger Logger);
