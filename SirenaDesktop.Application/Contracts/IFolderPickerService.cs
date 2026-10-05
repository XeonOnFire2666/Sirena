using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Application.Contracts;
public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string title, string? startPath = null);
}
