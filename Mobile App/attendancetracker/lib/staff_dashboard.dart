import 'dart:async';
import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:geolocator/geolocator.dart';
import 'package:attendancetracker/login_page.dart';

class StaffDashboard extends StatefulWidget {
  @override
  _StaffDashboardState createState() => _StaffDashboardState();
}

class _StaffDashboardState extends State<StaffDashboard> {
  late HubConnection _hubConnection;
  bool isPresent = false;
  bool isAbsent = false;
  bool isManualUpdate = false;
  bool isSignalRConnected = false;
  bool isLoading = false;
  List<Map<String, String>> _attendanceData = [];
  List<Map<String, dynamic>> _locationHistory = [];

  int _tapCount = 0;
  Timer? _resetTapTimer;

  @override
  void initState() {
    super.initState();
    initSignalR();
  }

  Future<void> UpdateStaffGrid() async {
  _hubConnection.on("UpdateStaffGrid", (arguments) async {
    if (arguments != null && arguments.isNotEmpty) {
      var data = arguments[0];

      if (data is List) {
        setState(() {
          if (isManualUpdate) {
            // Update only the last item (or matched one)
            if (_attendanceData.isNotEmpty) {
              final lastIndex = _attendanceData.length - 1;
              _attendanceData[lastIndex]['status'] =
                  isPresent ? 'Present' : isAbsent ? 'Absent' : 'Unknown';
            }
          } else {
            // Normal SignalR update: full clear and rebuild
            _attendanceData.clear();

            for (var item in data) {
              final map = Map<String, dynamic>.from(item);
              final name = map['name'] ?? 'Unknown';
              final status = map['status'] ?? 'Unknown';

              _attendanceData.add({
                'name': name.toString(),
                'status': status.toString(),
              });
            }
          }
        });

        print("✅ Updated Attendance Data from SignalR: $_attendanceData");
      }
    } else {
      print("⚠️ No data received in UpdateStaffGrid.");
    }

    // Reset flags
    setState(() {
      isManualUpdate = false;
      isPresent = false;
      isAbsent = false;
    });
  });
}

  Future<void> initSignalR() async {
    _hubConnection =
        HubConnectionBuilder()
            .withUrl("http://192.168.1.14:5248/attendanceHub")
            .withAutomaticReconnect()
            .build();

    try {
      await _hubConnection.start();
      setState(() => isSignalRConnected = true);
      print("✅ SignalR Connected!");

      await UpdateStaffGrid();

      final box = await Hive.openBox('sessionBox');
      final userType = box.get('userType')?.toString().toLowerCase();

      if (userType == 'staff') {
        final staffInfo = box.get('staffInfo');
        final staffName = staffInfo['Name'];

        Position position = await Geolocator.getCurrentPosition();
        double lat = position.latitude;
        double long = position.longitude;

        await _hubConnection.invoke(
          "RegisterStaff",
          args: [staffName, lat, long],
        );
        print("🟢 RegisterStaff called for $staffName at ($lat, $long)");
      }
    } catch (e) {
      print("❌ SignalR connection or registration failed: $e");
    }
  }

  Future<void> sendAttendanceRequest() async {
    var box = await Hive.openBox('sessionBox');
    var userInfo = box.get('studentInfo');

    if (userInfo == null) {
      print("❌ Staff info not found in Hive.");
      return;
    }

    setState(() => isLoading = true);

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        setState(() => isLoading = false);
        print("❌ Location permission denied.");
        return;
      }
    }

    try {
      if (_hubConnection.state != HubConnectionState.Connected) {
        print("⚠️ SignalR not connected. Attempting reconnect...");
        await _hubConnection.start();
        print("🔄 Reconnected to SignalR.");
      }

      Position position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );
      _locationHistory.add({
        'latitude': position.latitude,
        'longitude': position.longitude,
        'timestamp': DateTime.now().toIso8601String(),
      });
      await _hubConnection.invoke(
        "RegisterStaff",
        args: [userInfo['Name'], position.latitude, position.longitude],
      );

      await _hubConnection.invoke(
        "SendAttendanceRequest",
        args: [userInfo['Name']],
      );

      print("📡 Attendance request sent.");
    } catch (e) {
      print("❌ Error invoking SignalR methods: $e");
    }

    setState(() => isLoading = false);
  }

  Future<void> logout() async {
    var box = await Hive.openBox('sessionBox');
    await box.clear();
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (_) => LoginPage()),
      (_) => false,
    );
  }

  void handleAppBarTap() {
    _tapCount++;
    isManualUpdate = true;
    if (_tapCount == 2) {
      print("✅ Double Tap detected: Mark last entry Present");
      setState(() {
        isPresent = true;
        isAbsent = false;
      });
    } else if (_tapCount == 3) {
      print("❌ Triple Tap detected: Mark last entry Absent");
      setState(() {
        isPresent = false;
        isAbsent = true;
      });

      _tapCount = 0;
      _resetTapTimer?.cancel();
      return;
    }

    _resetTapTimer?.cancel();
    _resetTapTimer = Timer(Duration(milliseconds: 500), () {
      _tapCount = 0;
    });
  }

  @override
  void dispose() {
    _hubConnection.stop();
    _resetTapTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Color(0xFFF7F7F7),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 1,
        iconTheme: IconThemeData(color: Colors.black87),
        title: GestureDetector(
          onTap: handleAppBarTap,
          child: Text(
            'Staff Dashboard',
            style: TextStyle(
              color: Colors.black87,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: isLoading ? null : sendAttendanceRequest,
          ),
          IconButton(
            icon: Icon(Icons.logout),
            tooltip: 'Logout',
            onPressed: logout,
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.black87,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                onPressed: isLoading ? null : sendAttendanceRequest,
                icon:
                    isLoading
                        ? SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2,
                          ),
                        )
                        : Icon(Icons.send),
                label: Text("Send Attendance Request"),
              ),
            ),
            SizedBox(height: 20),
            Text(
              "📍 Location History",
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
            ),
            SizedBox(height: 10),
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black12,
                    blurRadius: 4,
                    offset: Offset(0, 2),
                  ),
                ],
              ),
              child:
                  _locationHistory.isEmpty
                      ? Padding(
                        padding: const EdgeInsets.all(16.0),
                        child: Center(
                          child: Text("No location data available"),
                        ),
                      )
                      : ListView.builder(
                        shrinkWrap: true,
                        physics: NeverScrollableScrollPhysics(),
                        itemCount: _locationHistory.length,
                        itemBuilder: (context, index) {
                          final loc = _locationHistory[index];
                          return ListTile(
                            leading: Icon(
                              Icons.location_on,
                              color: Colors.blueGrey,
                            ),
                            title: Text(
                              "Lat: ${loc['latitude']}, Lng: ${loc['longitude']}",
                              style: TextStyle(fontSize: 14),
                            ),
                            subtitle: Text("Time: ${loc['timestamp']}"),
                          );
                        },
                      ),
            ),
            SizedBox(height: 20),
            Text(
              "📝 Attendance Overview",
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
            ),
            SizedBox(height: 10),
            Expanded(
              child:
                  _attendanceData.isEmpty
                      ? Center(child: Text("No attendance data available"))
                      : Container(
                        width: double.infinity,
                        padding: EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black12,
                              blurRadius: 4,
                              offset: Offset(0, 2),
                            ),
                          ],
                        ),
                        child: SingleChildScrollView(
                          scrollDirection: Axis.vertical,
                          child: DataTable(
                            headingRowColor: MaterialStateColor.resolveWith(
                              (states) => Colors.blueGrey[50]!,
                            ),
                            headingTextStyle: TextStyle(
                              fontWeight: FontWeight.bold,
                              color: Colors.black87,
                            ),
                            columnSpacing: 24,
                            dataRowHeight: 56,
                            columns: [
                              DataColumn(label: Text('Name')),
                              DataColumn(label: Text('Status')),
                            ],
                            rows:
                                _attendanceData.map((student) {
                                  final isPresent =
                                      student['status'] == 'Present';
                                  return DataRow(
                                    cells: [
                                      DataCell(Text(student['name'] ?? '')),
                                      DataCell(
                                        Text(
                                          student['status'] ?? '',
                                          style: TextStyle(
                                            color:
                                                isPresent
                                                    ? Colors.green
                                                    : Colors.red,
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                      ),
                                    ],
                                    color: MaterialStateProperty.resolveWith<
                                      Color?
                                    >((Set<MaterialState> states) {
                                      return isPresent
                                          ? Colors.green[50]
                                          : Colors.red[50];
                                    }),
                                  );
                                }).toList(),
                          ),
                        ),
                      ),
            ),
          ],
        ),
      ),
    );
  }
}
