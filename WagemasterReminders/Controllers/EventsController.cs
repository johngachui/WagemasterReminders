using Microsoft.AspNetCore.Mvc;
using YourProjectName.Models;
using YourProjectName.Services;

namespace YourProjectName.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;

        public EventsController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // POST: api/Events
        [HttpPost]
        public ActionResult<IEnumerable<Event>> GetEvents([FromBody] UserLogin userLogin)
        {
            // Here, GetEvents reads multiple database paths from INI file and checks each of them for the user.
            var events = _databaseService.GetEvents(userLogin.Username, userLogin.Password);
            if (events == null || events.Count == 0)
            {
                return Unauthorized();
            }

            return Ok(events);
        }



        [HttpPost("update/{id}")]
        public ActionResult UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            // Check if user is authenticated using HttpContext.Items
            if (HttpContext.Items["User"] is not User user)
                return Unauthorized();

            if (!_databaseService.UpdateEvent(id, request.Dismissed, request.Username, request.DatabasePath, request.Password))
                return NotFound();

            return Ok();
        }


    }

}
