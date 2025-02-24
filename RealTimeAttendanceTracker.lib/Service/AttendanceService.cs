using Microsoft.EntityFrameworkCore;
using RealTimeAttendanceTracker.lib.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeAttendanceTracker.lib.Service
{
    public class AttendanceService
    {
        #region login
        public async Task<Login> ValidateLoginAsync(string email, string password)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = await db.Logins.FirstOrDefaultAsync(x => x.Email == email && x.Password == password);
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new Login();
            }
        }
        #endregion
        #region studentcreation
        public async Task<bool> AddUpdateStudentsAsync(Student student)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = await db.Students.FirstOrDefaultAsync(x => x.Id == student.Id);
                    var regNoValidation = await db.Students.AsNoTracking().CountAsync(x => x.RegNo == student.RegNo);
                    if (regNoValidation > 1)
                    {
                        return false;
                    }
                    if (data != null)
                    {
                        data.Name = student.Name;
                        data.RegNo = student.RegNo;
                        data.Degree = student.Degree;
                        data.Year = student.Year;
                        data.DateOfBirth = student.DateOfBirth;
                        data.ModifiedAt = DateTime.Now;
                    }
                    else
                    {
                        await db.Students.AddAsync(student);
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<List<Student>> GetStudentsAsync(int id = 0)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = db.Students.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();
                    if (id > 0)
                    {
                        data = data.Where(x => x.Id == id);
                    }
                    var result = await data.ToListAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<Student>();
            }
        }
        public async Task<bool> DeleteStudentAsync(int id)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = await db.Students.FirstOrDefaultAsync(x => x.Id == id);
                    if (data != null)
                    {
                        data.IsDeleted = !data.IsDeleted;
                        await db.SaveChangesAsync();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
        #region staffcreation
        public async Task<bool> AddUpdateStaffAsync(Staff staff)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = await db.Staffs.FirstOrDefaultAsync(x => x.Id == staff.Id);
                    if (data != null)
                    {
                        data.StaffName = staff.StaffName;
                        data.Department = staff.Department;
                        data.HandlingSubjects = string.Concat(",", staff.HandlingSubjects);
                        data.ModifiedAt = DateTime.Now;
                    }
                    else
                    {
                        await db.Staffs.AddAsync(staff);
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<List<Staff>> GetStaffsAsync(int id = 0)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = db.Staffs.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();
                    if (id > 0)
                    {
                        data = data.Where(x => x.Id == id);
                    }
                    var result = await data.ToListAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<Staff>();
            }
        }
        public async Task<bool> DeleteStaffAsync(int id)
        {
            try
            {
                using(var db = new AttendanceContext())
                {
                    var data = await db.Staffs.FirstOrDefaultAsync(x=>x.Id == id);
                    if(data != null)
                    {
                        data.IsDeleted = !data.IsDeleted;
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
            }catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
    }
}
