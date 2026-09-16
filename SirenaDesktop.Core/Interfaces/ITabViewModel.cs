using SirenaDesktop.Core.Enums;

namespace SirenaDesktop.Core.Interfaces;

public interface ITabViewModel
{
    string Title { get; }
    IconType Icon { get; }
}