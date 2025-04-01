using Microsoft.EntityFrameworkCore;
using RealTimeAttendanceTracker.lib.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RealTimeAttendanceTracker.lib.Service
{
    public class AttendanceService
    {
        #region login
        public async Task<Login> ValidateLoginAsync(string email, string password, string userType ="")
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    if (!string.IsNullOrEmpty(userType))
                    {
                        var result = await db.Logins.FirstOrDefaultAsync(x => x.Email == email && x.Password == password && x.Role == userType && !x.IsDeleted);
                        return result;
                    }
                    var data = await db.Logins.FirstOrDefaultAsync(x => x.Email == email && x.Password == password && !x.IsDeleted);
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
                        data.HandlingSubjects = data.HandlingSubjects;
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
                using (var db = new AttendanceContext())
                {
                    var data = await db.Staffs.FirstOrDefaultAsync(x => x.Id == id);
                    if (data != null)
                    {
                        data.IsDeleted = !data.IsDeleted;
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
        #endregion
        #region timetable
        public async Task<bool> AddTimeTableAsync(Dictionary<string, List<string>> data)
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var timeTableEntries = new List<Timetable>();

                    foreach (var dayEntry in data) // Loop through days
                    {
                        string day = dayEntry.Key; 
                        List<string> subjects = dayEntry.Value; // List of subjects for the day

                        for (int i = 0; i < subjects.Count; i++) // Loop through 8 periods
                        {
                            if (!string.IsNullOrEmpty(subjects[i]) && subjects[i] != "LUNCH") // Only save if subject is not empty
                            {
                                timeTableEntries.Add(new Timetable
                                {
                                    Subject = subjects[i],
                                    Time = i + 1, // Period starts from 1
                                    Year = "3rd",
                                    Day = day
                                });
                            }
                        }
                    }
                    await db.Timetables.AddRangeAsync(timeTableEntries);
                    await db.SaveChangesAsync();
                    return true;
                }
                
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<Dictionary<string, List<string>>> GetTimeTableAsync()
        {
            try
            {
                using (var db = new AttendanceContext())
                {
                    var data = await db.Timetables.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();
                    List<string> days = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

                    var timetable = new Dictionary<string, List<string>>();

                    foreach (var day in days)
                    {
                        var subjects = new string[9]; // Create array of 8 empty slots
                        Array.Fill(subjects, ""); // Default all periods to empty strings

                        var periods = data.Where(t => t.Day == day).ToList();

                        foreach (var period in periods)
                        {
                            if (period.Time >= 1 && period.Time <= 8) // Ensure period is within valid range
                            {
                                subjects[period.Time - 1] = period.Subject ?? ""; // Assign subject or empty string
                            }
                        }

                        // Ensure the 6th period is "LUNCH"
                        subjects[5] = "LUNCH";

                        timetable[day] = subjects.ToList();
                    }

                    return timetable;

                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
        #region login
        public async Task<bool> AddUpdateLoginAsync(Login login)
        {
            try
            {
                using(var db = new AttendanceContext())
                {
                    var data = await db.Logins.FirstOrDefaultAsync(x => x.Id == login.Id);
                    if(data != null)
                    {
                        data.Email = login.Email;   
                        data.Password = login.Password;
                        data.Role = login.Role;
                    }
                    else
                    {
                        await db.Logins.AddAsync(login);
                    }
                    await db.SaveChangesAsync();    
                    return true;
                }
            }catch(Exception ex)
            {
                return false;
            }
        }
        public async Task<List<Login>> GetLoginsAsync(string id = "")
        {
            using(var db = new AttendanceContext())
            {
                var data = db.Logins.AsNoTracking().Where(x=>!x.IsDeleted).AsQueryable();
                if (!string.IsNullOrEmpty(id))
                {
                    int primaryKey = Convert.ToInt32(id);
                    data = data.Where(x => x.Id == primaryKey);
                }
                var result = await data.ToListAsync();
                return result;
            }
        }

        public async Task<bool> DeleteLoginAsync(int id)
        {
            try
            {
                using(var db = new AttendanceContext())
                {
                    var data = await db.Logins.FirstOrDefaultAsync(x => x.Id == id);
                    if(data != null)
                    {
                        data.IsDeleted = !data.IsDeleted;
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
            }catch(Exception ex)
            {
                return false;
            }
        }
        #endregion
    }
}
