﻿using Geopilot.Api.Controllers;
using Geopilot.Api.DTOs;
using Geopilot.Api.Models;
using Geopilot.Api.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geopilot.Api.Test.Controllers
{
    [TestClass]
    public class MandateControllerTest
    {
        private Mock<ILogger<MandateController>> loggerMock;
        private Mock<IValidationService> validationServiceMock;
        private Context context;
        private MandateController mandateController;
        private User editUser;
        private User adminUser;
        private Mandate unrestrictedMandate;
        private Mandate xtfMandate;
        private Mandate unassociatedMandate;

        [TestInitialize]
        public void Initialize()
        {
            loggerMock = new Mock<ILogger<MandateController>>();
            validationServiceMock = new Mock<IValidationService>();
            context = AssemblyInitialize.DbFixture.GetTestContext();
            mandateController = new MandateController(loggerMock.Object, context, validationServiceMock.Object);

            unrestrictedMandate = new Mandate { FileTypes = new string[] { ".*" }, Name = nameof(unrestrictedMandate) };
            xtfMandate = new Mandate { FileTypes = new string[] { ".xtf" }, Name = nameof(xtfMandate) };
            unassociatedMandate = new Mandate { FileTypes = new string[] { "*.itf" }, Name = nameof(unassociatedMandate) };

            context.Mandates.Add(unrestrictedMandate);
            context.Mandates.Add(xtfMandate);
            context.Mandates.Add(unassociatedMandate);

            editUser = new User { AuthIdentifier = "123", FullName = "Edit User" };
            context.Users.Add(editUser);
            adminUser = new User { AuthIdentifier = "1234", FullName = "Admin User", IsAdmin = true };
            context.Users.Add(adminUser);

            var tempOrg = new Organisation { Name = "TestOrg" };
            tempOrg.Mandates.Add(unrestrictedMandate);
            tempOrg.Mandates.Add(xtfMandate);
            tempOrg.Users.Add(editUser);
            tempOrg.Users.Add(adminUser);

            context.Add(tempOrg);
            context.SaveChanges();
        }

        [TestMethod]
        public async Task GetReturnsListOfMandatesForUser()
        {
            mandateController.SetupTestUser(editUser);

            var result = (await mandateController.Get()) as OkObjectResult;
            var mandates = (result?.Value as IEnumerable<MandateDto>)?.ToList();

            Assert.IsNotNull(mandates);
            ContainsMandate(mandates, MandateDto.FromMandate(unrestrictedMandate));
            ContainsMandate(mandates, MandateDto.FromMandate(xtfMandate));
            DoesNotContainMandate(mandates, MandateDto.FromMandate(unassociatedMandate));
        }

        [TestMethod]
        public async Task GetWithJobIdIncludesMatchingMandates()
        {
            var jobId = Guid.NewGuid();
            mandateController.SetupTestUser(editUser);
            validationServiceMock
                .Setup(m => m.GetJob(jobId))
                .Returns(new ValidationJob(jobId, "Original.xtf", "tmp.xtf"));

            var result = (await mandateController.Get(jobId)) as OkObjectResult;
            var mandates = (result?.Value as IEnumerable<MandateDto>)?.ToList();

            Assert.IsNotNull(mandates);
            ContainsMandate(mandates, MandateDto.FromMandate(unrestrictedMandate));
            ContainsMandate(mandates, MandateDto.FromMandate(xtfMandate));
            DoesNotContainMandate(mandates, MandateDto.FromMandate(unassociatedMandate));
        }

        [TestMethod]
        public async Task GetWithJobIdExcludesNonMatchinMandates()
        {
            var jobId = Guid.NewGuid();
            mandateController.SetupTestUser(editUser);
            validationServiceMock
                .Setup(m => m.GetJob(jobId))
                .Returns(new ValidationJob(jobId, "Original.csv", "tmp.csv"));

            var result = (await mandateController.Get(jobId)) as OkObjectResult;
            var mandates = (result?.Value as IEnumerable<MandateDto>)?.ToList();

            Assert.IsNotNull(mandates);
            ContainsMandate(mandates, MandateDto.FromMandate(unrestrictedMandate));
            DoesNotContainMandate(mandates, MandateDto.FromMandate(xtfMandate));
            DoesNotContainMandate(mandates, MandateDto.FromMandate(unassociatedMandate));
        }

        [TestMethod]
        public async Task GetWithInvalidJobIdReturnsEmptyArray()
        {
            var jobId = Guid.NewGuid();
            mandateController.SetupTestUser(editUser);
            validationServiceMock
                .Setup(m => m.GetJob(jobId))
                .Returns(() => null);

            var result = (await mandateController.Get(jobId)) as OkObjectResult;
            var mandates = (result?.Value as IEnumerable<MandateDto>)?.ToList();

            Assert.IsNotNull(mandates);
            Assert.AreEqual(0, mandates.Count);
        }

        [TestMethod]
        public async Task GetWithoutValidDbUserReturnUnauthorized()
        {
            mandateController.SetupTestUser(new User { AuthIdentifier = "NotRegisteredUserId" });

            var result = await mandateController.Get();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task CreateMandate()
        {
            mandateController.SetupTestUser(adminUser);
            var mandate = new MandateDto() { FileTypes = new string[] { ".*" }, Name = "Test create", Organisations = new List<int>() { 1 } };
            var result = await mandateController.Create(mandate).ConfigureAwait(false);
            ActionResultAssert.IsCreated(result);
            var resultValue = (result as CreatedResult)?.Value as MandateDto;
            Assert.IsNotNull(resultValue);

            var getMandatesResult = await mandateController.Get().ConfigureAwait(false) as OkObjectResult;
            var mandates = getMandatesResult?.Value as IEnumerable<MandateDto>;
            Assert.IsNotNull(mandates);
            ContainsMandate(mandates, resultValue);
        }

        [TestMethod]
        public async Task EditMandate()
        {
            mandateController.SetupTestUser(adminUser);
            var mandate = new MandateDto() { FileTypes = new string[] { ".*", ".zip" }, Name = "Test update", Organisations = new List<int>() { 1, 2 }, Deliveries = new List<int>() { 1 } };
            var result = await mandateController.Create(mandate).ConfigureAwait(false) as CreatedResult;

            var updatedMandate = result?.Value as MandateDto;
            Assert.IsNotNull(updatedMandate);
            updatedMandate.Name = "Updated name";
            updatedMandate.FileTypes = new string[] { ".zip", ".gpkg" };
            updatedMandate.Organisations = new List<int>() { 2, 3 };

            var updateResult = await mandateController.Edit(updatedMandate).ConfigureAwait(false);
            ActionResultAssert.IsOk(updateResult);
            var resultValue = (updateResult as OkObjectResult)?.Value as MandateDto;
            Assert.IsNotNull(resultValue);

            var getMandatesResult = await mandateController.Get().ConfigureAwait(false) as OkObjectResult;
            var mandates = getMandatesResult?.Value as IEnumerable<MandateDto>;
            Assert.IsNotNull(mandates);
            ContainsMandate(mandates, updatedMandate);
        }

        [TestCleanup]
        public void Cleanup()
        {
            context.Dispose();
            loggerMock.VerifyAll();
            validationServiceMock.VerifyAll();
        }

        private void ContainsMandate(IEnumerable<MandateDto> mandates, MandateDto mandate)
        {
            var found = mandates.FirstOrDefault(m => m.Id == mandate.Id);
            Assert.IsNotNull(found);
            CompareMandates(mandate, found);
        }

        private void DoesNotContainMandate(IEnumerable<MandateDto> mandates, MandateDto mandate)
        {
            var found = mandates.FirstOrDefault(m => m.Id == mandate.Id);
            Assert.IsNull(found);
        }

        private void CompareMandates(MandateDto expected, MandateDto actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            CollectionAssert.AreEqual(expected.FileTypes, actual.FileTypes);
            CollectionAssert.AreEqual(expected.Organisations, actual.Organisations);
            CollectionAssert.AreEqual(expected.Deliveries, actual.Deliveries);
        }
    }
}
