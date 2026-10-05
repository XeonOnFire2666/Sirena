using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Application.Contracts.Todo;
public record TodoDtoOut(
    Guid Id,
    string Title,
    DateTime? StartTime,
    DateTime? EndTime,
    DateTime? CompleteDate,
    bool IsCompleted,
    bool IsDeleted,
    DateTime UpdatedAt
);