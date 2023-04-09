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

        // GET: api/Events
        [HttpGet]
        public ActionResult<IEnumerable<Event>> GetEvents()
        {
            return Ok(_databaseService.GetEvents());
        }
    }
}
