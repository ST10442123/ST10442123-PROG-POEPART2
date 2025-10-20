using CMCS1.Data;
using CMCS1.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CMCS1.Tests
{
    public static class TestHelpers
    {
        public static ApplicationDbContext InMemoryDb(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new ApplicationDbContext(options);
        }

        public static IWebHostEnvironment FakeEnv(string? root = null)
        {
            var mock = new Mock<IWebHostEnvironment>();
            mock.SetupGet(e => e.WebRootPath)
                .Returns(root ?? Path.Combine(Path.GetTempPath(), "cmcs1_tests_wwwroot"));
            Directory.CreateDirectory(mock.Object.WebRootPath);
            return mock.Object;
        }

        public static void AttachTempData(object controller)
        {
            // Attach the temp data to a controller so flashes work in tests
            var http = new DefaultHttpContext();
            var tempProvider = new Mock<ITempDataProvider>();
            var dict = new TempDataDictionary(http, tempProvider.Object);

            var tempProp = controller.GetType().GetProperty("TempData");
            tempProp?.SetValue(controller, dict);
        }
    }
}
