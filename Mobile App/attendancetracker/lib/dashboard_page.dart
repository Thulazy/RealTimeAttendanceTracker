import 'dart:async';
import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:geolocator/geolocator.dart';
import 'package:attendancetracker/login_page.dart';

class DashboardPage extends StatefulWidget {
  @override
  _DashboardPageState createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  late HubConnection _hubConnection;
  List<Map<String, dynamic>> studentAttendanceList = [];
  bool isSignalRConnected = false;
  bool isLoading = false;
  Timer? _connectionMonitorTimer;
  String userRole = "";

  @override
  void initState() {
    super.initState();
    _initSignalR();
    _connectionMonitorTimer = Timer.periodic(Duration(seconds: 5), (_) {
      if (mounted) {
        setState(() {
          isSignalRConnected =
              _hubConnection.state == HubConnectionState.Connected;
        });
      }
    });
  }

  void _logout(BuildContext context) async {
    var box = await Hive.openBox('sessionBox');
    await box.clear();

    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (context) => LoginPage()),
      (route) => false,
    );

    print("✅ User logged out successfully.");
  }

  Future<void> _initSignalR() async {
    _hubConnection =
        HubConnectionBuilder()
            .withUrl("http://192.168.1.5:5248/attendanceHub")
            .withAutomaticReconnect()
            .build();

    _hubConnection.on("ReceiveAttendanceResponse", (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        var data = arguments[0];

        // If backend sends a Map (ideal for structured data)
        if (data is Map) {
          final name = data["name"] ?? "Unknown";
          final status = data["status"] ?? "Unknown";

          if (mounted) {
            setState(() {
              studentAttendanceList.add({"name": name, "status": status});
            });
          }

          print("✅ Received response: $name - $status");
        } else {
          print("⚠️ Unexpected data format in attendance response: $data");
        }
      } else {
        print("⚠️ No data received in attendance response.");
      }
    });

    try {
      await _hubConnection.start();
      setState(() {
        isSignalRConnected = true;
      });
      print("✅ SignalR Connected!");
    } catch (e) {
      print("❌ SignalR Connection Failed: $e");
    }
  }

  Future<void> _sendAttendanceRequest() async {
    var box = await Hive.openBox('sessionBox');
    var userInfo = box.get('studentInfo');

    setState(() {
      isLoading = true;
    });

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        setState(() {
          isLoading = false;
        });
        print("❌ Location permission denied by user.");
        return;
      }
    }

    try {
      Position staffPosition = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );

      await _hubConnection.invoke(
        "RegisterStaff",
        args: [
          userInfo['Name'],
          staffPosition.latitude,
          staffPosition.longitude,
        ],
      );

      await _hubConnection.invoke(
        "SendAttendanceRequest",
        args: [userInfo['Name']],
      );

      print("📡 Attendance request sent.");
    } catch (e) {
      print("❌ Error getting location or invoking: $e");
    }

    setState(() {
      isLoading = false;
    });
  }

  @override
  void dispose() {
    _hubConnection.stop();
    _connectionMonitorTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder(
      valueListenable: Hive.box('sessionBox').listenable(),
      builder: (context, Box box, _) {
        var userInfo = box.get('studentInfo') ?? {};
        var userName = userInfo['Name'] ?? "User";
        userRole = box.get('role') ?? "";  // Retrieve the role from session

        return Scaffold(
          appBar: AppBar(
            title: Text("Dashboard"),
            actions: [
              IconButton(
                icon: Icon(Icons.logout),
                onPressed: () => _logout(context),
              ),
            ],
          ),
          drawer: Drawer(
            child: ListView(
              padding: EdgeInsets.zero,
              children: [
                UserAccountsDrawerHeader(
                  accountName: Text(userName),
                  accountEmail: Text("Logged in"),
                  currentAccountPicture: CircleAvatar(
                    child: Text(userName[0]),
                    backgroundColor: Colors.white,
                  ),
                  decoration: BoxDecoration(color: Colors.blue),
                ),
                ListTile(
                  leading: Icon(Icons.dashboard),
                  title: Text("Dashboard"),
                  onTap: () => Navigator.pop(context),
                ),
                ListTile(
                  leading: Icon(Icons.settings),
                  title: Text("Settings"),
                  onTap: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
          body: userRole.toLowerCase() == "student"
              ? buildStudentPersonalDetails(userInfo)
              : buildStaffDashboard(),
        );
      },
    );
  }

  // Build personal details grid for students
  Widget buildStudentPersonalDetails(Map<String, dynamic> userInfo) {
    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: GridView.builder(
        itemCount: userInfo.length,
        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          crossAxisSpacing: 16,
          mainAxisSpacing: 16,
        ),
        itemBuilder: (context, index) {
          String key = userInfo.keys.elementAt(index);
          var value = userInfo[key];
          return Card(
            elevation: 4,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  key,
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                SizedBox(height: 4),
                Text(
                  value.toString(),
                  style: TextStyle(fontSize: 14),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  // Build staff dashboard with attendance grid
  Widget buildStaffDashboard() {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(8.0),
          child: Text(
            isSignalRConnected
                ? "✅ Connected to Server"
                : "⚠️ Not Connected",
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: isSignalRConnected ? Colors.green : Colors.red,
            ),
          ),
        ),
        ElevatedButton(
          onPressed: isLoading ? null : _sendAttendanceRequest,
          child: isLoading
              ? CircularProgressIndicator(color: Colors.white)
              : Text("Get Attendance"),
        ),
        Expanded(
          child: studentAttendanceList.isEmpty
              ? Center(child: Text("No Attendance Data Yet"))
              : GridView.builder(
                  padding: EdgeInsets.all(16),
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    crossAxisSpacing: 16,
                    mainAxisSpacing: 16,
                  ),
                  itemCount: studentAttendanceList.length,
                  itemBuilder: (context, index) {
                    var student = studentAttendanceList[index];
                    return Card(
                      elevation: 4,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(
                            student["name"],
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            student["status"],
                            style: TextStyle(
                              fontSize: 14,
                              color: student["status"] == "Present"
                                  ? Colors.green
                                  : Colors.red,
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}
