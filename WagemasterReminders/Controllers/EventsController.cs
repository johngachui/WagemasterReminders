using Microsoft.AspNetCore.Mvc;
//using WagemasterAPI.Models;
using YourProjectName.Models;
using YourProjectName.Services;

namespace YourProjectName.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WagemasterController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IReminderService _reminderService;

        public WagemasterController(IDatabaseService databaseService, IReminderService reminderService)
        {
            _databaseService = databaseService;
            _reminderService = reminderService;
        }

        
        // POST: api/wagemaster/Events - Events
        [HttpPost("events")]
        public ActionResult<IEnumerable<Event>> GetEvents([FromBody] UserLogin userLogin)
        {
            //Refresh repeating records in TASKS then the API REMINDERS table
            _ = _reminderService.UpdateReminders(userLogin.Username, userLogin.Password);
            
            // Here, GetEvents reads multiple database paths from INI file and checks each of them for the user.
            var events = _databaseService.GetEvents(userLogin.Username, userLogin.Password);
            if (events == null || events.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(events);
        }

        // POST: api/wagemaster/Events/update/id - Events
        [HttpPost("events/update/{id}")]
        public ActionResult UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {

            if (!_databaseService.UpdateEvent(id, request.Dismissed, request.DatabasePath, request.Username, request.Password, request.Ref_ID, request.ReminderType))
                return NotFound();

            return Ok();
        }

        // POST: api/wagemaster/leavebals - LeaveBals
        [HttpPost("leavebals")]
        public ActionResult<IEnumerable<LeaveBals>> GetLeaveBals([FromBody] LeaveEmployee leaveEmployee)
        {
            // Here, GetLeaveBals reads multiple database paths from INI file and checks each of them for the user.
            var leavebals = _databaseService.GetLeaveBals(leaveEmployee.Num,leaveEmployee.CompanyKey,leaveEmployee.DivisionKey, leaveEmployee.EmployeeKey);
            if (leavebals == null || leavebals.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(leavebals);
        }

        // POST: api/wagemaster/leavedays - LeaveDays
        [HttpPost("leavedays")]
        public ActionResult<IEnumerable<LeaveDays>> GetLeaveDays([FromBody] LeaveEmployee leaveEmployee)
        {
            // Here, GetLeaveBals reads multiple database paths from INI file and checks each of them for the user.
            var leavedays = _databaseService.GetLeaveDays(leaveEmployee.Num, leaveEmployee.CompanyKey, leaveEmployee.DivisionKey,leaveEmployee.EmployeeKey);
            if (leavedays == null || leavedays.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(leavedays);
        }

        // POST: api/wagemaster/leavedays/application - LeaveDays
        [HttpPost("leavedays/application")]
        public ActionResult CreateLeaveApplication([FromBody] LeaveApplications leaveappl)
        {
            
            if (!_databaseService.CreateLeaveApplication(leaveappl.Num, leaveappl.StartDate, leaveappl.StopDate, leaveappl.LeaveType, leaveappl.CompanyKey, leaveappl.DivisionKey, leaveappl.EmployeeKey))
                return NotFound();

            return Ok();
        }

        // POST: api/wagemaster/hrmaster - hrmaster
        [HttpPost("hrmaster")]
        public ActionResult<IEnumerable<HR_Master>> GetHRMaster([FromBody] HREmployee employee)
        {
            // Here, GetLeaveBals reads multiple database paths from INI file and checks each of them for the user.
            var hrmasteremps = _databaseService.GetHRMaster(employee.Num,employee.CompanyKey,employee.DivisionKey, employee.EmployeeKey);
            if (hrmasteremps == null || hrmasteremps.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(hrmasteremps);
        }




    }

}
