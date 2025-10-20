using System.Linq;
using System.Threading.Tasks;
using CMCS1.Controllers;
using CMCS1.Data;
using CMCS1.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CMCS1.Tests
{
    public class ClaimsControllerTests
    {
        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            var hasAuthorize =
                typeof(ClaimsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .Any();

            hasAuthorize.Should().BeTrue("ClaimsController should require login via [Authorize]");
        }

        [Fact]
        public async Task Submit_AddsClaim_WhenModelValid()
        {
            using var ctx = TestHelpers.InMemoryDb(nameof(Submit_AddsClaim_WhenModelValid));
            var env = TestHelpers.FakeEnv();

            var controller = new ClaimsController(ctx, env);
            TestHelpers.AttachTempData(controller);

            var model = new Claim
            {
                LecturerName = "Test Lecturer",
                HoursWorked = 10,
                HourlyRate = 5,
                Notes = "Please approve"
            };

            var result = await controller.Submit(model, uploadedFile: null);

            // Should redirect back to Submit
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(ClaimsController.Submit));

            // DB should have one claim with Pending status
            ctx.Claims.Count().Should().Be(1);
            var saved = ctx.Claims.First();
            saved.Status.Should().Be(ClaimStatus.Pending);
            saved.TotalAmount.Should().Be(50);
        }

        [Fact]
        public void Review_ReturnsPendingClaims()
        {
            using var ctx = TestHelpers.InMemoryDb(nameof(Review_ReturnsPendingClaims));
            var env = TestHelpers.FakeEnv();
            var controller = new ClaimsController(ctx, env);

            ctx.Claims.AddRange(
                new Claim { LecturerName = "A", Status = ClaimStatus.Pending, HoursWorked = 1, HourlyRate = 1, DateSubmitted = DateTime.Now },
                new Claim { LecturerName = "B", Status = ClaimStatus.Approved, HoursWorked = 1, HourlyRate = 1, DateSubmitted = DateTime.Now },
                new Claim { LecturerName = "C", Status = ClaimStatus.Pending, HoursWorked = 1, HourlyRate = 1, DateSubmitted = DateTime.Now }
            );
            ctx.SaveChanges();

            var result = controller.Review() as ViewResult;
            result.Should().NotBeNull();

            var model = result!.Model as IEnumerable<Claim>;
            model.Should().NotBeNull();

            model!.Count().Should().Be(2);
            model!.All(c => c.Status == ClaimStatus.Pending).Should().BeTrue();
        }

        [Fact]
        public void Approve_ChangesStatusToApproved()
        {
            using var ctx = TestHelpers.InMemoryDb(nameof(Approve_ChangesStatusToApproved));
            var env = TestHelpers.FakeEnv();
            var controller = new ClaimsController(ctx, env);
            TestHelpers.AttachTempData(controller);

            var claim = new Claim
            {
                LecturerName = "X",
                Status = ClaimStatus.Pending,
                HoursWorked = 2,
                HourlyRate = 10,
                DateSubmitted = DateTime.Now
            };
            ctx.Claims.Add(claim);
            ctx.SaveChanges();

            var result = controller.Approve(claim.Id);

            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(ClaimsController.Review));

            ctx.Claims.Find(claim.Id)!.Status.Should().Be(ClaimStatus.Approved);
        }

        [Fact]
        public void Reject_ChangesStatusToRejected()
        {
            using var ctx = TestHelpers.InMemoryDb(nameof(Reject_ChangesStatusToRejected));
            var env = TestHelpers.FakeEnv();
            var controller = new ClaimsController(ctx, env);
            TestHelpers.AttachTempData(controller);

            var claim = new Claim
            {
                LecturerName = "Y",
                Status = ClaimStatus.Pending,
                HoursWorked = 3,
                HourlyRate = 20,
                DateSubmitted = DateTime.Now
            };
            ctx.Claims.Add(claim);
            ctx.SaveChanges();

            var result = controller.Reject(claim.Id);

            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be(nameof(ClaimsController.Review));

            ctx.Claims.Find(claim.Id)!.Status.Should().Be(ClaimStatus.Rejected);
        }
    }
}

