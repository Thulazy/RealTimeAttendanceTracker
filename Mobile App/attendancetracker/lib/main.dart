import 'package:attendancetracker/staff_dashboard.dart';
import 'package:attendancetracker/student_dashboard.dart';
import 'package:flutter/material.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'login_page.dart';
import 'dashboard_page.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  await Hive.initFlutter();
  var box = await Hive.openBox('sessionBox');

  bool isLoggedIn = box.get('isLoggedIn', defaultValue: false);
  String? userEmail = box.get('userEmail');
  String? userType = box.get('userType');
  Map<String, dynamic>? studentInfo =
      (box.get('studentInfo') as Map?)?.cast<String, dynamic>();

  runApp(
    MyApp(
      isLoggedIn: isLoggedIn,
      userEmail: userEmail,
      userType: userType,
      studentInfo: studentInfo,
    ),
  );
}

class MyApp extends StatefulWidget {
  final bool isLoggedIn;
  final String? userEmail;
  final String? userType;
  final Map<String, dynamic>? studentInfo;

  MyApp({
    required this.isLoggedIn,
    this.userEmail,
    this.userType,
    this.studentInfo,
  });

  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  late HubConnection _hubConnection;
  bool isSignalRConnected = false;
  final notificationPlugin = FlutterLocalNotificationsPlugin();
  bool _isInitialized = false;

  @override
  void initState() {
    super.initState();
    // Initialize the SignalR connection after the build phase
    Future.microtask(() async {
      await _initializeSignalR();
    });
  }

  /// Initialize SignalR connection
  Future<void> _initializeSignalR() async {
    try {
      _hubConnection =
          HubConnectionBuilder()
              .withUrl("http://192.168.1.14:5248/attendanceHub")
              .withAutomaticReconnect()
              .build();

      // Wait for the connection to start
      await _hubConnection.start();
      setState(() {
        isSignalRConnected = true;
      });

      print("✅ SignalR Connected!");

      // Register student if connected
      final box = await Hive.openBox('sessionBox');
      final studentInfo = box.get('studentInfo') as Map?;
      final studentName = studentInfo?['Name'];

      if (studentName != null &&
          _hubConnection.state == HubConnectionState.Connected) {
        await _hubConnection.invoke("RegisterStudent", args: [studentName]);
        print("🎓 Student registered on hub: $studentName");
      }

      // Listen for attendance requests
      _hubConnection.on("ReceiveAttendanceRequest", (arguments) {
        if (arguments != null && arguments.isNotEmpty) {
          String staffName = arguments[0]?.toString() ?? "Unknown Staff";
          _showNotification(
            "📢 Attendance Request",
            "$staffName is requesting attendance.",
          );
        }
      });

      // Listen for confirmation
      _hubConnection.on("AttendanceConfirmed", (arguments) {
        if (arguments != null && arguments.isNotEmpty) {
          String studentName = arguments[0]?.toString() ?? "Unknown Student";
          _showNotification(
            "✅ Attendance Confirmed",
            "$studentName has responded.",
          );
        }
      });
    } catch (e) {
      print("❌ SignalR Connection Error: $e");
    }
  }

  /// Show a local notification
  Future<void> _showNotification(String title, String message) async {
    const AndroidNotificationDetails
    androidDetails = AndroidNotificationDetails(
      'attendance_channel', // Channel ID
      'Attendance Notifications', // Channel name
      channelDescription: 'Notifications for attendance updates',
      importance: Importance.max,
      priority: Priority.high,
      ticker: 'ticker',
      icon:
          '@mipmap/ic_launcher', // Use the existing ic_launcher icon from mipmap
    );

    const NotificationDetails notificationDetails = NotificationDetails(
      android: androidDetails,
    );

    await notificationPlugin.show(
      0, // Notification ID
      title,
      message,
      notificationDetails,
    );
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      theme: ThemeData(
        fontFamily: 'SF Pro Display', // iPhone-style font if added via assets
        textTheme: const TextTheme(
          bodyMedium: TextStyle(fontSize: 16, fontFamily: 'SF Pro Display'),
        ),
      ),
      debugShowCheckedModeBanner: false,
      home:
          widget.isLoggedIn
              ? (widget.userType == "Staff"
                  ? StaffDashboard()
                  : StudentDashboard())
              : LoginPage(),
    );
  }

  @override
  void dispose() {
    if (isSignalRConnected) {
      _hubConnection.stop();
    }
    super.dispose();
  }
}
