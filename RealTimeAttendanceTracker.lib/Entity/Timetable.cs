using System;
using System.Collections.Generic;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Timetable
{
    public int Id { get; set; }

    public string Subject { get; set; } = null!;

    public string Time { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
