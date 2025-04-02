using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RealTimeAttendanceTracker.lib.Service;
using RealTimeAttendanceTracker.lib.Utility;
using System.Security.Claims;
using RealTimeAttendanceTracker.lib.Entity;
using Newtonsoft.Json;

namespace RealTimeAttendanceTracker.Web.Controllers
{
    public class MobileAppController : Controller
    {
        private readonly AttendanceService _attendanceSerivce;
        public MobileAppController(AttendanceService attendanceSerivce)
        {
            _attendanceSerivce = attendanceSerivce;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                string email = form["email"];
                string password = form["password"];
                string userType = form["userType"];

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return BadRequest(new { status = false, message = "Missing email or password." });
                }

                var result = await _attendanceSerivce.ValidateLoginAsync(email, password, userType);
                if (result != null)
                {
                    if(userType == "Student")
                    {
                        var userInfo = (await _attendanceSerivce.GetStudentsAsync(result.StudentsRefId ?? 0)).FirstOrDefault();
                        var response = new
                        {
                            status = true,
                            message = "Login successful",
                            data = new
                            {
                                id = result.Id,
                                email = result.Email,
                                role = result.Role,
                                userInfo = userInfo
                            }
                        };
                        return Ok(response); // Return JSON response
                    }
                    if (userType == "Staff")
                    {
                        var userInfo = (await _attendanceSerivce.GetStaffsAsync(result.StaffsRefId ?? 0)).FirstOrDefault();
                        var response = new
                        {
                            status = true,
                            message = "Login successful",
                            data = new
                            {
                                id = result.Id,
                                email = result.Email,
                                role = result.Role,
                                userInfo = userInfo
                            }
                        };
                        return Ok(response); // Return JSON response
                    }

                }

                return Unauthorized(new { status = false, message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoginAsync: {ex.Message}");
                return StatusCode(500, new { status = false, message = "An error occurred. Please try again later." });
            }
        }
    }
}
