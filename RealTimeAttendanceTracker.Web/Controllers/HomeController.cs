using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using RealTimeAttendanceTracker.lib.Entity;
using RealTimeAttendanceTracker.lib.Service;
using RealTimeAttendanceTracker.Web.Models;

namespace RealTimeAttendanceTracker.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AttendanceService _attendanceService;
        public HomeController(ILogger<HomeController> logger, AttendanceService attendanceService)
        {
            _logger = logger;
            _attendanceService = attendanceService;
        }

        public IActionResult Index()
        {
            return View();
        }
        #region students
        public async Task<IActionResult> AddUpdateStudentsAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                int primaryKey = Convert.ToInt32(id);
                var result = (await _attendanceService.GetStudentsAsync(primaryKey)).FirstOrDefault();
                if (result != null)
                {
                    return View(result);
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddUpdateStudentsAsync(Student student, bool isUpdate = false)
        {
            try
            {
                var result = await _attendanceService.AddUpdateStudentsAsync(student);
                if (result)
                {
                    TempData["Status"] = result;
                    if (isUpdate)
                    {
                        TempData["StatusMessage"] = "Students has been updated successfully.";
                        return RedirectToAction("AddUpdateStudents");
                    }
                    else
                    {
                        TempData["StatusMessage"] = "Student has been added successfully.";
                        return RedirectToAction("AddUpdateStudents");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["Status"] = false;
                return View(new Student { });
            }

        }

        public async Task<JsonResult> GetStudentsAsync()
        {
            var result = await _attendanceService.GetStudentsAsync();
            return Json(result);
        }
        public async Task<JsonResult> DeleteStudentAsync(int id)
        {
            var result = await _attendanceService.DeleteStudentAsync(id);
            return Json(result);
        }
        #endregion
        #region staff
        public async Task<IActionResult> AddUpdateStaffAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                int primaryKey = Convert.ToInt32(id);
                var result = (await _attendanceService.GetStaffsAsync(primaryKey)).FirstOrDefault();
                if (result != null)
                {
                    result.SubjectList = result.HandlingSubjects.Split(",").ToList();
                    return View(result);
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddUpdateStaffAsync(Staff staff, bool isUpdate = false)
        {
            try
            {
                staff.HandlingSubjects = string.Join(",", staff.SubjectList);
                var result = await _attendanceService.AddUpdateStaffAsync(staff);
                if (result)
                {
                    TempData["Status"] = result;
                    if (isUpdate)
                    {
                        TempData["StatusMessage"] = "Staff has been updated successfully.";
                        return RedirectToAction("AddUpdateStaff");
                    }
                    else
                    {
                        TempData["StatusMessage"] = "Staff has been added successfully.";
                        return RedirectToAction("AddUpdateStaff");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["Status"] = false;
                return View(new Staff { });
            }

        }
        public async Task<JsonResult> GetStaffsAsync()
        {
            var result = await _attendanceService.GetStaffsAsync();
            return Json(result);
        }
        public async Task<JsonResult> DeleteStaffAsync(int id)
        {
            var result = await _attendanceService.DeleteStaffAsync(id);
            return Json(result);
        }
        #endregion
        #region timetable
        public IActionResult AddUpdateTimeTable()
        {
            return View();
        }
        public async Task<JsonResult> AddTimeTableAsync(string data)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data);
            var result = await _attendanceService.AddTimeTableAsync(json);
            return Json(result);
        }
        public async Task<JsonResult> GetTimeTableAsync()
        {
            var result = await _attendanceService.GetTimeTableAsync();
            return Json(result);
        }
        #endregion
        #region login
        public async Task<IActionResult> AddUpdateLoginAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var result = (await _attendanceService.GetLoginsAsync(id)).FirstOrDefault();
                if(result != null)
                {
                    return View(result);
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddUpdateLoginAsync(Login login, bool isUpdate = false)
        {
            try
            {
                var result = await _attendanceService.AddUpdateLoginAsync(login);
                if (result)
                {
                    TempData["Status"] = result;
                    if (isUpdate)
                    {
                        TempData["StatusMessage"] = "Login has been updated successfully.";
                        return RedirectToAction("AddUpdateLogin");
                    }
                    else
                    {
                        TempData["StatusMessage"] = "Login has been added successfully.";
                        return RedirectToAction("AddUpdateLogin");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["Status"] = false;
                return View(new Login { });
            }

        }

        public async Task<JsonResult> GetLoginsAsync()
        {
            var result = await _attendanceService.GetLoginsAsync();
            return Json(result);
        }
        public async Task<JsonResult> DeleteLoginAsync(int id)
        {
            var result = await _attendanceService.DeleteLoginAsync(id);
            return Json(result);
        }
        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
