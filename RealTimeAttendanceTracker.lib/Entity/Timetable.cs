using System;
using System.Collections.Generic;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Timetable
{
    public int Id { get; set; }

    public string Subject { get; set; } = null!;

    public int Time { get; set; }
    public string? Year { get; set; }
    public string? Day {  get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; } = false;
}
