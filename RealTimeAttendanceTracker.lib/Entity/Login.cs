using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeAttendanceTracker.lib.Entity
{
    public class Login
    {
        public int Id { get; set; } 
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }   
        public int? StudentsRefId { get; set; }  
        public int? StaffsRefId {  get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        [NotMapped]
        public string? UserType { get; set; }
        public virtual Student StudentsRef { get; set; }
        public virtual Staff StaffsRef { get; set; }
    }
}
