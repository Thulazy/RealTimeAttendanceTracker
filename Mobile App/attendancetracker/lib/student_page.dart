import 'package:flutter/material.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:geolocator/geolocator.dart';
import 'package:hive/hive.dart';

class StudentPage extends StatefulWidget {
  @override
  _StudentPageState createState() => _StudentPageState();
}

class _StudentPageState extends State<StudentPage> {
  late HubConnection _hubConnection;

  @override
  void initState() {
    super.initState();
    _initSignalR();
  }

  void _initSignalR() {
    _hubConnection =
        HubConnectionBuilder()
            .withUrl("http://192.168.1.5:5248/attendanceHub")
            .build();

    _hubConnection.on("ReceiveAttendanceNotification", (staffName) {
      _showAttendanceDialog(staffName![0].toString());
    });

    _hubConnection.on("AttendanceConfirmed", (message) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(message![0].toString())));
    });

    _hubConnection.start();
  }

  void _showAttendanceDialog(String staffName) {
    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text("Attendance Confirmation"),
          content: Text("Do you want to mark attendance for $staffName?"),
          actions: [
            TextButton(
              child: Text("No"),
              onPressed: () {
                Navigator.pop(context);
              },
            ),
            TextButton(
              child: Text("Yes"),
              onPressed: () async {
                Navigator.pop(context);
                _sendStudentConfirmation(staffName);
              },
            ),
          ],
        );
      },
    );
  }

  Future<void> _sendStudentConfirmation(String staffName) async {
    // Retrieve student details from Hive
    var box = await Hive.openBox('sessionBox');
    String? studentName = box.get(
      'userEmail',
    ); // Use student email or name stored in Hive
    String? studentId = box.get(
      'userType',
    ); // Use the actual student ID if available

    if (studentName == null || studentId == null) {
      print("Error: Student details not found in Hive.");
      return;
    }

    Position position = await Geolocator.getCurrentPosition(
      desiredAccuracy: LocationAccuracy.high,
    );

    await _hubConnection.invoke(
      "ConfirmAttendance",
      args: [
        studentId, // Actual student ID
        studentName, // Actual student name
        position.latitude,
        position.longitude,
        staffName,
        12.9716, // Replace with actual staff latitude
        77.5946, // Replace with actual staff longitude
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text("Student Page")),
      body: Center(child: Text("Waiting for Attendance Request...")),
    );
  }
}
