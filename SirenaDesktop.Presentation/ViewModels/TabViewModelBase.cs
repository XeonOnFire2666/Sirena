using CommunityToolkit.Mvvm.ComponentModel;
using SirenaDesktop.Core.Enums;
using SirenaDesktop.Core.Interfaces;

namespace SirenaDesktop.ViewModels;

public abstract class TabViewModelBase : ObservableObject, ITabViewModel
{
    protected TabViewModelBase(string title, IconType icon)
    {
        Title = title;
        Icon = icon;
    }

    public string Title { get; }
    public IconType Icon { get; }
}