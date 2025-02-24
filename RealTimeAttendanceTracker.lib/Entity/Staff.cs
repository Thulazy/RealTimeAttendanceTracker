using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Staff
{
    public int Id { get; set; }

    public string StaffName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string HandlingSubjects { get; set; } = null!;
    [NotMapped]
    public List<string> SubjectList { get; set; } = new List<string>();

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }
}
