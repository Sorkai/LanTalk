using System.Collections.ObjectModel;
using LanTalk.Core.Constants;
using LanTalk.Core.Enums;

namespace LanTalk.App.ViewModels;

public sealed class ContactGroupViewModel : ViewModelBase
{
    public ContactGroupViewModel(string name, IEnumerable<OnlineUserViewModel> users)
    {
        Name = string.IsNullOrWhiteSpace(name) ? NetworkConstants.DefaultDepartment : name.Trim();

        foreach (var user in users)
        {
            Users.Add(user);
        }
    }

    public string Name { get; }

    public ObservableCollection<OnlineUserViewModel> Users { get; } = [];

    public int OnlineCount => Users.Count(user => user.Status == UserStatus.Online);

    public int TotalCount => Users.Count;

    public string Summary => $"{OnlineCount}/{TotalCount} 在线";
}
