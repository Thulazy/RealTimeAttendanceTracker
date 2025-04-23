using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RealTimeAttendanceTracker.lib.Entity;
using RealTimeAttendanceTracker.Web.Models;

public class AttendanceHub : Hub
{
    private static ConcurrentDictionary<string, string> StudentConnections = new();
    private static ConcurrentDictionary<string, string> StaffConnections = new();
    private static ConcurrentDictionary<string, (double Lat, double Long)> StaffLocations = new();
    private static List<Dictionary<string, Dictionary<string, string>>> AttendanceHistory = new();
    private static Dictionary<string, List<AttendanceRecord>> _staffAttendanceMap = new();
    private static readonly List<Point> ClassroomCorners = new List<Point>
{
    new Point(12.8197984, 79.7149611),  // South West
    new Point(12.8197960, 79.7149726),  // North West
    new Point(12.8197927, 79.7149697),  // North East
    new Point(12.8197974, 79.7149668)   // South East
};
    public enum GeofenceMode
    {
        PolygonOnly,
        RadiusOnly,
        PolygonWithRadiusFallback
    }
    public class Point
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        public Point(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client Connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception = null)
    {
        string connectionId = Context.ConnectionId;

        // Remove student or staff from connection lists
        var student = StudentConnections.FirstOrDefault(s => s.Value == connectionId);
        if (!string.IsNullOrEmpty(student.Key))
        {
            StudentConnections.TryRemove(student.Key, out _);
        }

        var staff = StaffConnections.FirstOrDefault(s => s.Value == connectionId);
        if (!string.IsNullOrEmpty(staff.Key))
        {
            StaffConnections.TryRemove(staff.Key, out _);
            StaffLocations.TryRemove(staff.Key, out _);
        }

        Console.WriteLine($"Client Disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterStudent(string studentName)
    {
        StudentConnections[studentName] = Context.ConnectionId;
        Console.WriteLine($"Student Registered: {studentName}");
    }

    public async Task RegisterStaff(string staffName, double staffLat, double staffLong)
    {
        StaffConnections[staffName] = Context.ConnectionId;
        StaffLocations[staffName] = (staffLat, staffLong);
        await Groups.AddToGroupAsync(Context.ConnectionId, staffName);
        Console.WriteLine($"Staff Registered: {staffName}");
    }

    public async Task SendAttendanceRequest(string staffName)
    {
        //TestStudentCoordinates();
        if (StaffLocations.TryGetValue(staffName, out var location))
        {
            double staffLat = location.Lat;
            double staffLong = location.Long;

            Console.WriteLine($"Sending Attendance Request from {staffName}");

            // Get active students' names
            var activeStudentNames = StudentConnections.Keys
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToList();

            foreach (var student in activeStudentNames)
            {
                if (StudentConnections.TryGetValue(student, out var connId))
                {
                    // Send notification to student with Yes/No option
                    await Clients.Client(connId).SendAsync("ReceiveAttendanceRequest", staffName, staffLat, staffLong);
                }
            }
        }
    }
    public async Task UpdateStudentStatus(string staffName, string studentName, string status)
    {
        try
        {
            var existingRecord = _staffAttendanceMap.ContainsKey(staffName)
                ? _staffAttendanceMap[staffName].FirstOrDefault(r => r.StudentName == studentName)
                : null;
            if (existingRecord != null)
            {
                existingRecord.Status = status;
                existingRecord.Timestamp = DateTime.Now;
            }
            var updatedRecord = new Dictionary<string, string>
{
    { "name", studentName },
    { "status", status }
};

            await Clients.Group(staffName).SendAsync("UpdateStaffGrid", updatedRecord);
        }
        catch (Exception e)
        {
            throw;
        }
    }
    public async Task StudentAttendanceResponse(string studentName, string staffName, double studentLat, double studentLong)
    {
        if (StaffLocations.TryGetValue(staffName, out var staffLocation))
        {
            // Apply geofencing logic here
            bool IsPresent = IsStudentInGeofence(studentLat, studentLong, GeofenceMode.PolygonWithRadiusFallback, 1.0);
            string status = IsPresent ? "Present" : "Absent";

            Console.WriteLine($"Student: {studentName}, Lat: {studentLat}, Long: {studentLong} -> {status}");

            var existingRecord = _staffAttendanceMap.ContainsKey(staffName)
                ? _staffAttendanceMap[staffName].FirstOrDefault(r => r.StudentName == studentName)
                : null;

            if (existingRecord != null)
            {
                existingRecord.Status = status;
                existingRecord.Timestamp = DateTime.Now;
            }
            else
            {
                var record = new AttendanceRecord
                {
                    StudentName = studentName,
                    Status = status,
                    Timestamp = DateTime.Now
                };

                if (!_staffAttendanceMap.ContainsKey(staffName))
                    _staffAttendanceMap[staffName] = new List<AttendanceRecord>();

                _staffAttendanceMap[staffName].Add(record);
            }

            await Clients.Caller.SendAsync("AttendanceConfirmed", $"Your attendance has been marked.");

            var updatedRecord = new Dictionary<string, string>
{
    { "name", studentName },
    { "status", status }
};

            await Clients.Group(staffName).SendAsync("UpdateStaffGrid", updatedRecord);
        }
    }

    // Master geofencing logic (polygon + radius)
    public static bool IsStudentInGeofence(double studentLat, double studentLng, GeofenceMode mode = GeofenceMode.PolygonWithRadiusFallback, double radiusInMeters = 2)
    {
        switch (mode)
        {
            case GeofenceMode.PolygonOnly:
                return IsInsidePolygon(studentLat, studentLng);
            case GeofenceMode.RadiusOnly:
                var (centerLat, centerLng) = GetClassroomCenter();
                return IsWithinRadius(studentLat, studentLng, centerLat, centerLng, radiusInMeters);
            case GeofenceMode.PolygonWithRadiusFallback:
            default:
                if (IsInsidePolygon(studentLat, studentLng))
                {
                    (centerLat, centerLng) = GetClassroomCenter();
                    return IsWithinRadius(studentLat, studentLng, centerLat, centerLng, radiusInMeters);
                }
                return false;
        }
    }

    public static bool IsInsidePolygon(double lat, double lng)
    {
        int count = ClassroomCorners.Count;
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            double latI = ClassroomCorners[i].Lat;
            double lngI = ClassroomCorners[i].Lng;
            double latJ = ClassroomCorners[j].Lat;
            double lngJ = ClassroomCorners[j].Lng;

            if (IsPointOnLineSegment(lat, lng, latI, lngI, latJ, lngJ))
                return true;

            bool intersect = ((lngI > lng) != (lngJ > lng)) &&
                             (lat < (latJ - latI) * (lng - lngI) / (lngJ - lngI) + latI);
            if (intersect)
                inside = !inside;
        }

        return inside;
    }

    private static bool IsPointOnLineSegment(double lat, double lng, double lat1, double lng1, double lat2, double lng2)
    {
        if (lng < Math.Min(lng1, lng2) || lng > Math.Max(lng1, lng2))
            return false;
        if (lat < Math.Min(lat1, lat2) || lat > Math.Max(lat1, lat2))
            return false;

        double crossProduct = (lat - lat1) * (lng2 - lng1) - (lat2 - lat1) * (lng - lng1);
        return Math.Abs(crossProduct) < 1e-10; 
    }

    // Calculate the center of classroom polygon
    private static (double lat, double lng) GetClassroomCenter()
    {
        double avgLat = ClassroomCorners.Average(p => p.Lat);
        double avgLng = ClassroomCorners.Average(p => p.Lng);
        return (avgLat, avgLng);
    }

    // Check if location is within radius using Haversine formula
    private static bool IsWithinRadius(double lat1, double lon1, double lat2, double lon2, double radiusMeters = 2)
    {
        var R = 6371000; // Earth's radius in meters
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c <= radiusMeters;
    }

    // Helper: Degrees to radians
    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000; // in meters
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }


    public async Task ReceiveAttendanceConfirmation(string staffName, double studentLat, double studentLong)
    {
        var studentName = GetStudentNameFromContext();

        Console.WriteLine($"✅ {studentName} confirmed attendance to {staffName} at location ({studentLat}, {studentLong})");

        // Send confirmation to staff
        if (StaffConnections.TryGetValue(staffName.Trim().ToLower(), out var staffConnId))
        {
            await Clients.Client(staffConnId).SendAsync("StudentConfirmedAttendance", studentName, studentLat, studentLong);
        }
    }

    private string GetStudentNameFromContext()
    {
        var name = Context?.GetHttpContext()?.Request.Query["name"].ToString();
        return name?.Trim().ToLower() ?? "unknown";
    }

    private void SaveAttendanceHistory(string staffName, string studentName, string status)
    {
        var record = new Dictionary<string, Dictionary<string, string>>
        {
            { staffName, new Dictionary<string, string> { { studentName, status } } }
        };
        AttendanceHistory.Add(record);
    }
    //public void TestStudentCoordinates()
    //{
    //    var studentCoordinates = new List<(double Lat, double Lng)>
    //{
    //    (12.8197898, 79.7149625),  // Should be inside
    //    (12.8197873, 79.7149677),  // Should be inside
    //    (12.8197865, 79.7149656),  // Should be inside
    //    (12.8197918, 79.7149707),  // Should be inside
    //    (12.8197784, 79.7149352)   // Should be outside
    //};

    //    foreach (var (studentLat, studentLong) in studentCoordinates)
    //    {
    //        var result = IsInsideBoundingBox(studentLat, studentLong) ? "Present" : "Absent";
    //        Console.WriteLine($"Student Lat: {studentLat}, Lon: {studentLong} -> {result}");
    //    }
    //}
}