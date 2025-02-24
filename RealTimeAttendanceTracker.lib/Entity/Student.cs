using System;
using System.Collections.Generic;

namespace RealTimeAttendanceTracker.lib.Entity;

public partial class Student
{
    public int Id { get; set; }

    public string RegNo { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Year { get; set; } = null!;

    public string Degree { get; set; } = null!;

    public string Mobile { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public string Address { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }
}
