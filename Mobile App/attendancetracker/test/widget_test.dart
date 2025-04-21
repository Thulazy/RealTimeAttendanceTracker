import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:attendancetracker/main.dart';
import 'package:hive_flutter/hive_flutter.dart';

void main() {
  setUpAll(() async {
    WidgetsFlutterBinding.ensureInitialized();
    await Hive.initFlutter();
    await Hive.openBox('sessionBox');
  });

  testWidgets('LoginPage should open first', (WidgetTester tester) async {
    var box = Hive.box('sessionBox');
    await box.clear(); // Ensure session is empty

    await tester.pumpWidget(MyApp(
      isLoggedIn: box.get('isLoggedIn', defaultValue: false),
      userEmail: box.get('userEmail'),
      userType: box.get('userType'),
      studentInfo: box.get('studentInfo'),
    ));

    expect(find.text("Login"), findsOneWidget); // Ensure LoginPage is shown first
  });

  testWidgets('DashboardPage should open after login', (WidgetTester tester) async {
    var box = Hive.box('sessionBox');

    // Set session data for logged-in user
    await box.put('isLoggedIn', true);
    await box.put('userEmail', box.get('userEmail'));
    await box.put('userType', box.get('userType')); // Example user type
    await box.put('studentInfo', box.get('studentInfo'));

    await tester.pumpWidget(MyApp(
      isLoggedIn: box.get('isLoggedIn'),
      userEmail: box.get('userEmail'),
      userType: box.get('userType'),
      studentInfo: box.get('studentInfo'),
    ));

    expect(find.text("Dashboard"), findsOneWidget); // Ensure DashboardPage is shown after login
  });
}
