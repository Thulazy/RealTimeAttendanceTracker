using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    public async Task RespondToAttendanceRequest(string studentName, bool isPresent, string staffName, double studentLat, double studentLong)
    {
        // Get staff's location
        if (StaffLocations.TryGetValue(staffName, out var staffLocation))
        {
            double staffLat = staffLocation.Lat;
            double staffLong = staffLocation.Long;

            // Calculate the distance between the student and staff using Haversine formula
            double distance = CalculateDistance(studentLat, studentLong, staffLat, staffLong);

            // Check if the student is within 10-20 meters distance
            bool isStudentNearStaff = distance >= 10 && distance <= 20;

            // Mark as present if within the range, otherwise absent
            string status = isStudentNearStaff ? "Present" : "Absent";

            if (isPresent && isStudentNearStaff)
            {
                status = "Present"; // Confirm present if student is within range
            }
            else
            {
                status = "Absent"; // Otherwise, mark absent
            }

            // Send the student's response (Yes/No) to the staff
            if (StaffConnections.TryGetValue(staffName, out var staffConnId))
            {
                await Clients.Client(staffConnId).SendAsync("ReceiveAttendanceResponse", studentName, status);
            }

            // Optional: Save the attendance history
            SaveAttendanceHistory(staffName, studentName, status);
        }
    }
    public async Task StudentAttendanceResponse(string studentName, string staffName, double studentLat, double studentLong)
    {
        // Get staff location (you must maintain a dictionary or store this when the staff registers location)
        if (StaffLocations.TryGetValue(staffName, out var staffLocation))
        {
            double staffLat = staffLocation.Lat;
            double staffLong = staffLocation.Long;

            double distance = CalculateDistance(studentLat, studentLong, staffLat, staffLong); // in meters
            string status = (distance <= 20) ? "Present" : "Absent";

            // Save or update attendance status in memory or DB
            var record = new AttendanceRecord
            {
                StudentName = studentName,
                Status = status,
                Timestamp = DateTime.Now
            };

            if (!_staffAttendanceMap.ContainsKey(staffName))
                _staffAttendanceMap[staffName] = new List<AttendanceRecord>();

            _staffAttendanceMap[staffName].Add(record);

            // Optionally notify student
            await Clients.Caller.SendAsync("AttendanceConfirmed", $"{studentName} marked as {status}");
            var formattedData = _staffAttendanceMap[staffName]
    .Select(record => new Dictionary<string, string>
    {
        { "name", record.StudentName },
        { "status", record.Status }
    })
    .ToList<object>();

            // Push update to staff dashboard
            await Clients.Group(staffName).SendAsync("UpdateStaffGrid", formattedData);
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000; // Radius of the earth in meters
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c; // Distance in meters
        return distance;
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
}
