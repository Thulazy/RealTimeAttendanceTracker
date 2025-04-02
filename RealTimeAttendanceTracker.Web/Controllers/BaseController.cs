using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealTimeAttendanceTracker.lib.Service;
using RealTimeAttendanceTracker.lib.Utility;

namespace RealTimeAttendanceTracker.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AttendanceService _attendanceService;
        public BaseController(AttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }
        public IActionResult Index()
        {
            
            return View();
        }

        protected async Task<List<SelectListItem>> GetStudentsAsync()
        {
            List<SelectListItem> items = new List<SelectListItem> { new SelectListItem { Selected = true, Text = "Select Students", Value = "" } };
            var result = (await _attendanceService.GetStudentsAsync())?.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })?.OrderBy(x => x.Text)?.ToList();
            if (result != null)
            {
                items.AddRange(result);
            }
            return items;
        }
        protected async Task<List<SelectListItem>> GetStaffsAsync()
        {
            List<SelectListItem> items = new List<SelectListItem> { new SelectListItem { Selected = true, Text = "Select Staff", Value = "" } };
            var result = (await _attendanceService.GetStaffsAsync())?.Select(x => new SelectListItem { Text = x.StaffName, Value = x.Id.ToString() })?.OrderBy(x => x.Text)?.ToList();
            if (result != null)
            {
                items.AddRange(result);
            }
            return items;
        }
    }
}
