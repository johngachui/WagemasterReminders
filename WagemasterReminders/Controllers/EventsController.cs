using Microsoft.AspNetCore.Mvc;
//using WagemasterAPI.Models;
using YourProjectName.Models;
using YourProjectName.Services;

namespace YourProjectName.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IReminderService _reminderService;

        public EventsController(IDatabaseService databaseService, IReminderService reminderService)
        {
            _databaseService = databaseService;
            _reminderService = reminderService;
        }

        
        // POST: api/Events - Events
        [HttpPost]
        public ActionResult<IEnumerable<Event>> GetEvents([FromBody] UserLogin userLogin)
        {
            //Refresh repeating records in TASKS then the API REMINDERS table
            _ = _reminderService.UpdateReminders(userLogin.DatabasePath, userLogin.Username, userLogin.Password);
            
            // Here, GetEvents reads multiple database paths from INI file and checks each of them for the user.
            var events = _databaseService.GetEvents(userLogin.Username, userLogin.Password);
            if (events == null || events.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(events);
        }

        // POST: api/Events - LeaveBals
        [HttpPost("leavebals")]
        public ActionResult<IEnumerable<LeaveBals>> GetLeaveBals([FromBody] LeaveEmployee leaveEmployee)
        {
            // Here, GetLeaveBals reads multiple database paths from INI file and checks each of them for the user.
            var leavebals = _databaseService.GetLeaveBals(leaveEmployee.Num,leaveEmployee.CompanyPath);
            if (leavebals == null || leavebals.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(leavebals);
        }


        [HttpPost("update/{id}")]
        public ActionResult UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            
            if (!_databaseService.UpdateEvent(id, request.Dismissed, request.DatabasePath, request.Username, request.Password,request.Ref_ID,request.ReminderType))
                return NotFound();

            return Ok();
        }


    }

}
