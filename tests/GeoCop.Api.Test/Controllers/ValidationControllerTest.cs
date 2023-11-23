﻿using Asp.Versioning;
using GeoCop.Api.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GeoCop.Api.Controllers;

[TestClass]
public sealed class ValidationControllerTest
{
    private Mock<ILogger<ValidationController>> loggerMock;
    private Mock<IValidationService> validationServiceMock;
    private Mock<ApiVersion> apiVersionMock;
    private Mock<IFormFile> formFileMock;
    private ValidationController controller;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<ValidationController>>();
        validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
        apiVersionMock = new Mock<ApiVersion>(MockBehavior.Strict, 9, 88, null!);
        formFileMock = new Mock<IFormFile>(MockBehavior.Strict);

        controller = new ValidationController(
            loggerMock.Object,
            validationServiceMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
        validationServiceMock.VerifyAll();
        apiVersionMock.VerifyAll();
        formFileMock.VerifyAll();
    }

    [TestMethod]
    public async Task UploadAsync()
    {
        var jobId = Guid.NewGuid();
        const string originalFileName = "BIZARRESCAN.xtf";
        formFileMock.SetupGet(x => x.Length).Returns(1234);
        formFileMock.SetupGet(x => x.FileName).Returns(originalFileName);
        formFileMock.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var validationJob = new ValidationJob(jobId, originalFileName, "TEMP.xtf");
        using var fileHandle = new FileHandle(validationJob.TempFileName, Stream.Null);

        validationServiceMock.Setup(x => x.IsFileExtensionSupportedAsync(".xtf")).Returns(Task.FromResult(true));
        validationServiceMock.Setup(x => x.CreateValidationJob(originalFileName)).Returns((validationJob, fileHandle));
        validationServiceMock
            .Setup(x => x.StartValidationJobAsync(validationJob))
            .Returns(Task.FromResult(new ValidationJobStatus(jobId)));

        var response = await controller.UploadAsync(apiVersionMock.Object, formFileMock.Object) as CreatedResult;

        Assert.IsInstanceOfType(response, typeof(CreatedResult));
        Assert.IsInstanceOfType(response!.Value, typeof(ValidationJobStatus));
        Assert.AreEqual(StatusCodes.Status201Created, response.StatusCode);
        Assert.AreEqual($"/api/v9/validation/{jobId}", response.Location);
        Assert.AreEqual(jobId, ((ValidationJobStatus)response.Value!).JobId);
    }

    [TestMethod]
    public async Task UploadAsyncForNull()
    {
        var response = await controller.UploadAsync(apiVersionMock.Object, null!) as ObjectResult;

        Assert.IsInstanceOfType(response, typeof(ObjectResult));
        Assert.AreEqual(StatusCodes.Status400BadRequest, response!.StatusCode);
        Assert.AreEqual("Form data <file> cannot be empty.", ((ProblemDetails)response.Value!).Detail);
    }

    [TestMethod]
    public async Task UploadInvalidFileExtension()
    {
        formFileMock.SetupGet(x => x.FileName).Returns("upload.exe");

        validationServiceMock.Setup(x => x.IsFileExtensionSupportedAsync(".exe")).Returns(Task.FromResult(false));

        var response = await controller.UploadAsync(apiVersionMock.Object, formFileMock.Object) as ObjectResult;

        Assert.IsInstanceOfType(response, typeof(ObjectResult));
        Assert.AreEqual(StatusCodes.Status400BadRequest, response!.StatusCode);
        Assert.AreEqual("File extension <.exe> is not supported.", ((ProblemDetails)response.Value!).Detail);
    }

    [TestMethod]
    public void GetStatus()
    {
        var jobId = Guid.NewGuid();

        validationServiceMock
            .Setup(x => x.GetJobStatus(jobId))
            .Returns(new ValidationJobStatus(jobId) { Status = Status.Processing });

        var response = controller.GetStatus(jobId) as OkObjectResult;

        Assert.IsInstanceOfType(response, typeof(OkObjectResult));
        Assert.IsInstanceOfType(response!.Value, typeof(ValidationJobStatus));
        Assert.AreEqual(StatusCodes.Status200OK, response.StatusCode);
        Assert.AreEqual(jobId, ((ValidationJobStatus)response.Value!).JobId);
        Assert.AreEqual(Status.Processing, ((ValidationJobStatus)response.Value).Status);
    }

    [TestMethod]
    public void GetStatusForInvalid()
    {
        var jobId = Guid.Empty;

        validationServiceMock
            .Setup(x => x.GetJobStatus(Guid.Empty))
            .Returns((ValidationJobStatus?)null);

        var response = controller.GetStatus(jobId) as ObjectResult;

        Assert.IsInstanceOfType(response, typeof(ObjectResult));
        Assert.AreEqual(StatusCodes.Status404NotFound, response!.StatusCode);
        Assert.AreEqual($"No job information available for job id <{jobId}>", ((ProblemDetails)response.Value!).Detail);
    }
}
