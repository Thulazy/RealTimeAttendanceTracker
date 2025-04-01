using Microsoft.AspNetCore.Mvc;
using RealTimeAttendanceTracker.lib.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RealTimeAttendanceTracker.lib.Utility;
using RealTimeAttendanceTracker.lib.Entity;

namespace RealTimeAttendanceTracker.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly AttendanceService _attendanceSerivce;
        public LoginController(AttendanceService attendanceSerivce)
        {
            _attendanceSerivce = attendanceSerivce;
        }
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext?.User?.Identity?.IsAuthenticated != null && HttpContext.User.Identity.IsAuthenticated)
            {
                var ClaimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
                ClaimsIdentity.Claims.ToList().ForEach(x => HttpContext.Session.SetString(x.Type.ToString(), x.Value));
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IndexAsync(Login user)
        {
            try
            {
                var result = await _attendanceSerivce.ValidateLoginAsync(user.Email, user.Password);
                if (result != null)
                {
                    ViewBag.Status = true;
                    var claims = new List<Claim>
        {
            new Claim(AppConstants.SessionKeys.Id, result.Id.ToString()),
            new Claim(AppConstants.SessionKeys.Email, result.Email),
            new Claim(AppConstants.SessionKeys.Role, result.Role)
        };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    claimsIdentity.Claims.ToList().ForEach(x => HttpContext.Session.SetString(x.Type.ToString(), x.Value.ToString()));
                    var authProperties = new AuthenticationProperties
                    {
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Index", "Home");

                }
                ViewBag.Status = false;
                return View();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<IActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();
            return View();
        }
    }
}
