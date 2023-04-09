using Xunit;
using YourProjectName.Controllers;
using YourProjectName.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace YourTestProjectName
{
    public class UnitTest1
    {
        [Fact]
        public void TestGetEvents()
        {
            // Arrange
            EventsController controller = new EventsController();

            // Act
            ActionResult<IEnumerable<Event>> result = controller.GetEvents();

            // Assert
            Assert.NotNull(result.Value);
            Assert.NotEmpty(result.Value);
        }
    }
}
