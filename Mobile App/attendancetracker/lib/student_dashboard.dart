import 'package:flutter/material.dart';
import 'package:hive/hive.dart';
import 'package:attendancetracker/login_page.dart';
import 'package:flutter/services.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:geolocator/geolocator.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:permission_handler/permission_handler.dart';
import 'dart:io';

class StudentDashboard extends StatefulWidget {
  @override
  _StudentDashboardState createState() => _StudentDashboardState();
}

class _StudentDashboardState extends State<StudentDashboard> {
  Map<String, dynamic> _studentInfo = {};
  late HubConnection _hubConnection;
  late FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin;
  List<Map<String, dynamic>> _locationHistory = [];

  String _lastStaffName = '';
  double? _staffLatitude;
  double? _staffLongitude;
  @override
  void initState() {
    super.initState();
    _loadStudentInfo();
    _initSignalR();
    _initNotifications();
  }

  Future<void> _initNotifications() async {
    flutterLocalNotificationsPlugin = FlutterLocalNotificationsPlugin();

    const AndroidInitializationSettings androidSettings =
        AndroidInitializationSettings('@mipmap/ic_launcher');

    final InitializationSettings initializationSettings =
        InitializationSettings(android: androidSettings);

    // Initialize with callback
    await flutterLocalNotificationsPlugin.initialize(
      initializationSettings,
      onDidReceiveNotificationResponse: (NotificationResponse response) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            _onNotificationTapped(response);
          }
        });
      },
    );

    // Also check for app launch via notification
    final details =
        await flutterLocalNotificationsPlugin.getNotificationAppLaunchDetails();
    if (details?.didNotificationLaunchApp ?? false) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          _onNotificationTapped(details!.notificationResponse!);
        }
      });
    }

    _requestPermissions();
  }

  Future<void> _requestPermissions() async {
    if (Platform.isAndroid) {
      if (await Permission.notification.isDenied ||
          await Permission.notification.isRestricted) {
        await Permission.notification.request();
      }
    } else if (Platform.isIOS) {
      await Permission.notification.request();
    }
  }

  Future<void> _loadStudentInfo() async {
    var box = await Hive.openBox('sessionBox');
    setState(() {
      _studentInfo = Map<String, dynamic>.from(
        box.get('studentInfo', defaultValue: {}),
      );
    });
  }

  void _initSignalR() async {
    _hubConnection =
        HubConnectionBuilder()
            .withUrl("http://192.168.1.14:5248/attendanceHub")
            .withAutomaticReconnect()
            .build();

    _hubConnection.on("ReceiveAttendanceRequest", (staffData) async {
      if (staffData != null && staffData.length >= 3) {
        String staffName = staffData[0].toString();
        double lat = double.tryParse(staffData[1].toString()) ?? 0;
        double long = double.tryParse(staffData[2].toString()) ?? 0;
        _lastStaffName = staffName;
        _staffLatitude = lat;
        _staffLongitude = long;
        _showNotification(
          'Attendance Request',
          'Tap to mark attendance for $staffName',
        );
      }
    });

    _hubConnection.on("AttendanceConfirmed", (message) {
      if (message != null && message.isNotEmpty) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(message[0].toString())));
      }
    });

    try {
      await _hubConnection.start();

      final box = await Hive.openBox('sessionBox');
      final userType = box.get('userType')?.toString().toLowerCase();

      if (userType == 'student') {
        final studentInfo = box.get('studentInfo');
        final studentName = studentInfo['Name'];
        await _hubConnection.invoke("RegisterStudent", args: [studentName]);
        print("🟢 RegisterStudent called for $studentName");
      }
    } catch (e) {
      print("❌ SignalR connection or registration failed: $e");
    }
  }

  Future<void> _logout() async {
    var box = await Hive.openBox('sessionBox');
    await box.clear();
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (context) => LoginPage()),
      (route) => false,
    );
  }

  void _showNotification(String title, String body) async {
    const AndroidNotificationDetails androidDetails =
        AndroidNotificationDetails(
          'attendance_channel',
          'Attendance Notifications',
          channelDescription: 'Notifications for attendance updates',
          importance: Importance.max,
          priority: Priority.high,
          ticker: 'ticker',
        );

    const NotificationDetails notificationDetails = NotificationDetails(
      android: androidDetails,
    );

    await flutterLocalNotificationsPlugin.show(
      0,
      title,
      body,
      notificationDetails,
    );
  }

  void _onNotificationTapped(NotificationResponse response) {
    if (_lastStaffName.isNotEmpty) {
      _showAttendanceDialog(_lastStaffName);
    }
  }

  void _showAttendanceDialog(String staffName) {
    if (mounted) {
      showDialog(
        context: context,
        builder:
            (context) => AlertDialog(
              title: Text("Mark Attendance"),
              content: Text("Do you want to mark attendance for $staffName?"),
              actions: [
                TextButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: Text("No"),
                ),
                ElevatedButton(
                  onPressed: () async {
                    Navigator.of(context).pop();
                    await _checkLocationAndSendAttendance();
                  },
                  child: Text("Yes"),
                ),
              ],
            ),
      );
    }
  }

  Future<void> _checkLocationAndSendAttendance() async {
    LocationPermission permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      print("❌ Location permission denied by user.");
      return;
    }

    try {
      Position studentPosition = await Geolocator.getCurrentPosition();

      if (_staffLatitude != null && _staffLongitude != null) {
        _locationHistory.add({
          'latitude': studentPosition.latitude,
          'longitude': studentPosition.longitude,
          'timestamp': DateTime.now().toIso8601String(),
        });
        await _hubConnection.invoke(
          "StudentAttendanceResponse",
          args: [
            _studentInfo['Name'],
            _lastStaffName,
            studentPosition.latitude,
            studentPosition.longitude
          ],
        );
        print("📡 StudentAttendanceResponse sent successfully");
      } else {
        print("⚠️ Staff location is not available.");
      }
    } catch (e) {
      print("❌ Error retrieving student location: $e");
    }
  }

  @override
  Widget build(BuildContext context) {
    final isWide = MediaQuery.of(context).size.width > 600;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Student Dashboard'),
        actions: [
          IconButton(icon: const Icon(Icons.logout), onPressed: _logout),
        ],
      ),
      body: SafeArea(
        child:
            _studentInfo.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : SingleChildScrollView(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        "Personal Details",
                        style: TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 16),
                      GridView.count(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        crossAxisCount: isWide ? 3 : 2,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16,
                        childAspectRatio: 1,
                        children:
                            _studentInfo.entries.map((entry) {
                              return Container(
                                decoration: BoxDecoration(
                                  color:
                                      Colors
                                          .white, // White background for the card
                                  borderRadius: BorderRadius.circular(16),
                                  boxShadow: [
                                    BoxShadow(
                                      color:
                                          Colors
                                              .black12, // Soft shadow for depth
                                      blurRadius: 8,
                                      spreadRadius: 2,
                                      offset: Offset(0, 4),
                                    ),
                                  ],
                                ),
                                padding: const EdgeInsets.all(16),
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.center,
                                  children: [
                                    Text(
                                      entry.key,
                                      style: TextStyle(
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold,
                                        color:
                                            Colors
                                                .black, // Black text for labels
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                    const SizedBox(height: 8),
                                    Text(
                                      entry.value.toString(),
                                      style: TextStyle(
                                        fontSize: 14,
                                        color:
                                            Colors
                                                .black87, // Slightly lighter black text for values
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                  ],
                                ),
                              );
                            }).toList(),
                      ),
                      const SizedBox(height: 32),
                      const Text(
                        "📍 Location History",
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 12),
                      if (_locationHistory.isEmpty)
                        const Text("No location records yet.")
                      else
                        ListView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: _locationHistory.length,
                          itemBuilder: (_, index) {
                            final loc = _locationHistory[index];
                            return Container(
                              margin: const EdgeInsets.symmetric(vertical: 6),
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: Colors.grey.shade200),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withOpacity(0.05),
                                    blurRadius: 5,
                                    offset: const Offset(0, 3),
                                  ),
                                ],
                              ),
                              child: Row(
                                children: [
                                  const Icon(
                                    Icons.location_on_outlined,
                                    color: Colors.black54,
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          "Lat: ${loc['latitude']}, Lng: ${loc['longitude']}",
                                        ),
                                        Text(
                                          "Time: ${loc['timestamp']}",
                                          style: const TextStyle(
                                            color: Colors.black45,
                                            fontSize: 13,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            );
                          },
                        ),
                    ],
                  ),
                ),
      ),
    );
  }
}
