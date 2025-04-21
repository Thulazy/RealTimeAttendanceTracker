import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import 'dashboard_page.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:geolocator/geolocator.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'student_dashboard.dart';
import 'staff_dashboard.dart';

class LoginPage extends StatefulWidget {
  @override
  _LoginPageState createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final TextEditingController _emailController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();
  String _userType = 'Student';
  String _errorMessage = '';
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    Hive.initFlutter();
  }

  Future<void> _login() async {
    setState(() {
      _isLoading = true;
      _errorMessage = '';
    });
    String email = _emailController.text.trim();
    String password = _passwordController.text.trim();
    String userType = _userType;

    String url = 'http://192.168.1.14:5248/MobileApp/Login';
    Map<String, String> headers = {
      "Content-Type": "application/x-www-form-urlencoded",
    };

    String body = "email=$email&password=$password&userType=$userType";

    try {
      final response = await http.post(
        Uri.parse(url),
        headers: headers,
        body: body,
      );

      if (response.statusCode == 200) {
        var responseData = jsonDecode(response.body);

        if (responseData["status"] == true) {
          var data = responseData["data"];
          var box = await Hive.openBox('sessionBox');

          await box.put('isLoggedIn', true);
          await box.put('userEmail', data["email"]);
          await box.put('userType', data["role"]);

          final hubConnection =
              HubConnectionBuilder()
                  .withUrl("http://192.168.1.14:5248/attendanceHub")
                  .withAutomaticReconnect()
                  .build();

          try {
            await hubConnection.start();
            print("✅ SignalR connected.");
          } catch (e) {
            print("❌ Failed to connect to SignalR: $e");
          }

          if (data["role"].toLowerCase() == "student") {
            Map<String, dynamic> userInfo = {
              'Name': data['userInfo']['name'],
              'RegNo': data['userInfo']['regNo'],
              'Degree': data['userInfo']['degree'],
              'Year': data['userInfo']['year'],
              'Mobile': data['userInfo']['mobile'],
              'DateOfBirth': data['userInfo']['dob'],
              'Address': data['userInfo']['address'],
            };
            await box.put('studentInfo', userInfo);

            try {
              await hubConnection.invoke(
                "RegisterStudent",
                args: [userInfo['Name']],
              );
            } catch (e) {
              print("❌ Error registering student: $e");
            }
            setState(() {
              _isLoading = false;
            });
            Navigator.pushReplacement(
              context,
              MaterialPageRoute(builder: (context) => StudentDashboard()),
            );
          }

          if (data["role"].toLowerCase() == "staff") {
            Map<String, dynamic> userInfo = {
              'Name': data['userInfo']['staffName'],
              'Department': data['userInfo']['department'],
              'HandlingSubjects': data['userInfo']['handlingSubjects'],
            };
            await box.put('studentInfo', userInfo);

            try {
              LocationPermission permission =
                  await Geolocator.checkPermission();
              if (permission == LocationPermission.denied ||
                  permission == LocationPermission.deniedForever) {
                permission = await Geolocator.requestPermission();
              }

              if (permission == LocationPermission.whileInUse ||
                  permission == LocationPermission.always) {
                Position position = await Geolocator.getCurrentPosition(
                  desiredAccuracy: LocationAccuracy.high,
                );

                await hubConnection.invoke(
                  "RegisterStaff",
                  args: [
                    userInfo['Name'],
                    position.latitude,
                    position.longitude,
                  ],
                );
                setState(() {
                  _isLoading = false;
                });
              }
            } catch (e) {
              print("❌ Error registering staff: $e");
            }

            Navigator.pushReplacement(
              context,
              MaterialPageRoute(builder: (context) => StaffDashboard()),
            );
          }
        } else {
          setState(() {
            // Customize message if needed
            if (responseData["message"]?.toLowerCase().contains("invalid") ==
                true) {
              _errorMessage =
                  "Invalid credentials. Please check your email or password.";
            } else {
              _errorMessage =
                  responseData["message"] ?? "Login failed. Please try again.";
            }
          });
        }
      } else {
        setState(() {
          _errorMessage = "Server error: ${response.statusCode}";
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = "Network error: $e";
      });
    }
    setState(() {
      _isLoading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[100],
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 30.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.lock_outline, size: 80, color: Colors.grey[800]),
              SizedBox(height: 20),
              Text(
                'Welcome Back',
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  color: Colors.grey[900],
                ),
              ),
              SizedBox(height: 10),
              Text(
                'Login to your account',
                style: TextStyle(color: Colors.grey[600]),
              ),
              SizedBox(height: 30),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Radio<String>(
                    value: 'Student',
                    groupValue: _userType,
                    onChanged: (value) {
                      setState(() => _userType = value!);
                    },
                  ),
                  Text('Student'),
                  SizedBox(width: 20),
                  Radio<String>(
                    value: 'Staff',
                    groupValue: _userType,
                    onChanged: (value) {
                      setState(() => _userType = value!);
                    },
                  ),
                  Text('Staff'),
                ],
              ),
              SizedBox(height: 20),
              TextField(
                controller: _emailController,
                decoration: InputDecoration(
                  hintText: 'Email',
                  prefixIcon: Icon(Icons.email_outlined),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  filled: true,
                  fillColor: Colors.white,
                ),
              ),
              SizedBox(height: 15),
              TextField(
                controller: _passwordController,
                obscureText: true,
                decoration: InputDecoration(
                  hintText: 'Password',
                  prefixIcon: Icon(Icons.lock_outline),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  filled: true,
                  fillColor: Colors.white,
                ),
              ),
              SizedBox(height: 25),
              _isLoading
                  ? CircularProgressIndicator()
                  : ElevatedButton(
                    onPressed: _login,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.black54,
                      padding: EdgeInsets.symmetric(
                        horizontal: 50,
                        vertical: 15,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                    child: Text(
                      'Login',
                      style: TextStyle(
                        fontSize: 18,
                        color: Colors.white,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
              SizedBox(height: 15),
              if (_errorMessage.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(top: 12.0),
                  child: Text(
                    _errorMessage,
                    style: TextStyle(color: Colors.red),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
