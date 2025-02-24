using System;
using System.Collections.Generic;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Timetable
{
    public int Id { get; set; }

    public string Subject { get; set; } = null!;

    public string Time { get; set; } = null!;

    public int StudentsRefId { get; set; }

    public int StaffsRefId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public ulong IsDeleted { get; set; }
}
