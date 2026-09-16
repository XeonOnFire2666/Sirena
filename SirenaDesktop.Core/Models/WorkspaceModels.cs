using CommunityToolkit.Mvvm.ComponentModel;
using SirenaDesktop.Core.Enums;

namespace SirenaDesktop.Core.Models;

public class WorkspaceType : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IconType Icon { get; init; }

    public bool SupportsGitTracking { get; init; }

    public static List<WorkspaceType> GetDefaultTypes()
    {
        return
        [
            new WorkspaceType
            {
                Id = "software-engineering",
                Title = "Software Engineering",
                Description = "Development, code, project tasks",
                Icon = IconType.CodeBraces,
                SupportsGitTracking = true
            },
            new WorkspaceType
            {
                Id = "study",
                Title = "Study",
                Description = "Courses, lectures, exam preparation",
                Icon = IconType.SchoolOutline
            },
            new WorkspaceType
            {
                Id = "personal",
                Title = "Personal",
                Description = "Household, health, daily tasks",
                Icon = IconType.HomeOutline
            },
            new WorkspaceType
            {
                Id = "work",
                Title = "Work",
                Description = "Work tasks, meetings, deadlines",
                Icon = IconType.BriefcaseOutline
            },
            new WorkspaceType
            {
                Id = "creative",
                Title = "Creative",
                Description = "Projects, hobbies, creative tasks",
                Icon = IconType.PaletteOutline
            }
        ];
    }
}

public partial class DayWorkspace : ObservableObject
{
    [ObservableProperty] public partial DateOnly Date { get; set; }
    [ObservableProperty] public partial WorkspaceType? Type { get; set; }
}