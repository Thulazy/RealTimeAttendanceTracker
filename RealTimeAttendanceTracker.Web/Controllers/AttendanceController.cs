using Microsoft.AspNetCore.Mvc;

namespace RealTimeAttendanceTracker.Web.Controllers
{
    public class AttendanceController : ControllerBase
    {

        [HttpGet("history")]
        public IActionResult GetAttendanceHistory()
        {
            return Ok();
        }
    }
}
