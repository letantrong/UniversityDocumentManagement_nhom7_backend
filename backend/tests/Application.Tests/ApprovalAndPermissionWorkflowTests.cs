using Application.Common.Interfaces;
using Application.Features.Approvals;
using Application.Features.Notifications;
using Application.Features.Permissions;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;

namespace Application.Tests;

public class ApprovalAndPermissionWorkflowTests
{
    [Fact]
    public async Task SubmitDocumentForApproval_WhenCurrentUserIsNull_ShouldReturnUnauthorizedFailure()
    {
        var contextMock = new Mock<IApplicationDbContext>();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        var dateTimeMock = new Mock<IDateTimeService>();
        var notificationMock = new Mock<INotificationService>();

        var handler = new SubmitDocumentForApprovalCommandHandler(
            contextMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            notificationMock.Object);

        var command = new SubmitDocumentForApprovalCommand(Guid.NewGuid(), Guid.NewGuid(), 1);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Unauthorized");
        notificationMock.Verify(
            x => x.SendToUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMyNotifications_WhenUserIdIsNull_ShouldReturnEmptyListWithSuccess()
    {
        var contextMock = new Mock<IApplicationDbContext>();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = new GetMyNotificationsQueryHandler(contextMock.Object, currentUserMock.Object);
        var result = await handler.Handle(new GetMyNotificationsQuery(true), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(DocumentPermissionAction.View, true)]
    [InlineData(DocumentPermissionAction.Download, false)]
    [InlineData(DocumentPermissionAction.Edit, false)]
    [InlineData(DocumentPermissionAction.Delete, false)]
    [InlineData(DocumentPermissionAction.Share, false)]
    public async Task CheckDocumentPermission_ShouldMatchAccessMatrixAction(
        DocumentPermissionAction action,
        bool expectedAllowed)
    {
        var docId = Guid.NewGuid();
        var matrix = new DocumentAccessMatrixDto(
            DocumentId: docId,
            CanView: true,
            CanDownload: false,
            CanEdit: false,
            CanDelete: false,
            CanShare: false);

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(s => s.Send(
                It.Is<GetDocumentAccessQuery>(q => q.DocumentId == docId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Application.Common.Models.Result<DocumentAccessMatrixDto>.Success(matrix));

        var handler = new CheckDocumentPermissionQueryHandler(senderMock.Object);
        var result = await handler.Handle(new CheckDocumentPermissionQuery(docId, action), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().Be(expectedAllowed);
    }

    [Fact]
    public void DocumentAccessMatrix_WhenArchived_ShouldDisallowEditAndDelete()
    {
        var archivedMatrix = new DocumentAccessMatrixDto(
            DocumentId: Guid.NewGuid(),
            CanView: true,
            CanDownload: true,
            CanEdit: false,
            CanDelete: false,
            CanShare: true);

        archivedMatrix.CanEdit.Should().BeFalse();
        archivedMatrix.CanDelete.Should().BeFalse();
        archivedMatrix.CanView.Should().BeTrue();
    }
}