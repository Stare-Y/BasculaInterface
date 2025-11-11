using System;

namespace Core.Application.DTOs;

public record GetByDateRangeCommand
{
    public DateOnly startDate { get; init; }
    public DateOnly endDate { get; init; }
}
