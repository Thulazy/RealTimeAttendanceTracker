using System;
using System.Collections.Generic;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Staff
{
    public int Id { get; set; }

    public string StaffName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string HandlingSubjects { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public ulong IsDeleted { get; set; }
}
