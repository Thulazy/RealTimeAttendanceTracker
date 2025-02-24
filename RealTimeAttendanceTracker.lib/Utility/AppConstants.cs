using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeAttendanceTracker.lib.Utility
{
    public class AppConstants
    {
        public class AppSettingKey
        {
            public const string MySqlConn = "MySqlConn";
            public const string HostedUrl = "HostedUrl";
        }
        public class SessionKeys
        {
            public const string Id = "Id";
            public const string Email = "Email";
        }
    }
}
