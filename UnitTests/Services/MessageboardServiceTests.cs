using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class MessageboardServiceTests
{
    private readonly InMemoryApplicationDbContext _dbContext = new();
    private readonly Mock<IAvatarService> _avatarService = new();
    private readonly Mock<IMessageImageService> _messageImageService = new();
    private readonly Mock<IMessageboardNotifier> _notifier = new();
    private readonly Mock<IReactionCatalogue> _reactionCatalogue = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManager.Create();

    public MessageboardServiceTests()
    {
        // Every identity resolves to an image unless a test says otherwise - the catalogue's own
        // resolution rules are covered by ReactionCatalogueTests.
        _reactionCatalogue.Setup(c => c.ResolveImageFile(It.IsAny<string>())).Returns("1f44d.svg");
    }

    private MessageboardService MakeService()
        => new(_dbContext, _avatarService.Object, _messageImageService.Object, _notifier.Object, _reactionCatalogue.Object, _userManager.Object);

    private ApplicationUser AddViewer(bool canViewMessageboard = true, int totalPosts = 0)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "viewer", CanViewMessageboard = canViewMessageboard, TotalMessageboardPosts = totalPosts };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        return user;
    }

    private DomainEntities.MessageThread AddThread(bool hiddenFromPublic = false)
    {
        var thread = new DomainEntities.MessageThread
        {
            MessageThreadID = Guid.NewGuid(),
            ThreadSubject = "Subject",
            StartedByUserID = Guid.NewGuid(),
            StartedDateTime = DateTime.UtcNow,
            HiddenFromPublic = hiddenFromPublic,
        };
        _dbContext.MessageThread.Add(thread);
        _dbContext.SaveChanges();
        return thread;
    }

    [Fact]
    public async Task GetThreadsAsync_UserCannotViewMessageboard_ReturnsForbidden()
    {
        var user = AddViewer(canViewMessageboard: false);

        var result = await MakeService().GetThreadsAsync(user.Id, page: 1, pageSize: 30);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ForbiddenError);
    }

    [Fact]
    public async Task GetThreadsAsync_UnknownUser_ReturnsForbidden()
    {
        var result = await MakeService().GetThreadsAsync(Guid.NewGuid(), page: 1, pageSize: 30);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ForbiddenError);
    }

    [Fact]
    public async Task GetThreadsAsync_AllowedUser_Succeeds()
    {
        var user = AddViewer();

        var result = await MakeService().GetThreadsAsync(user.Id, page: 1, pageSize: 30);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetThreadAsync_UnknownThread_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().GetThreadAsync(Guid.NewGuid(), user.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task GetThreadAsync_HiddenThreadWithoutPermission_ReturnsNotFound()
    {
        var user = AddViewer();
        var thread = AddThread(hiddenFromPublic: true);

        var result = await MakeService().GetThreadAsync(thread.MessageThreadID, user.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task GetThreadAsync_HiddenThreadWithPermission_Succeeds()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin", CanViewMessageboard = true, CanViewHiddenMessageThreads = true };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        var thread = AddThread(hiddenFromPublic: true);

        var result = await MakeService().GetThreadAsync(thread.MessageThreadID, user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageThreadID.Should().Be(thread.MessageThreadID);
    }

    [Fact]
    public async Task GetMessagesAsync_ThreadNotFound_PropagatesFailure()
    {
        var user = AddViewer();

        var result = await MakeService().GetMessagesAsync(Guid.NewGuid(), user.Id, 20, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsMessagesOldestFirst()
    {
        var user = AddViewer();
        var thread = AddThread();
        var older = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow.AddMinutes(-10), MessageContent = "older" };
        var newer = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow, MessageContent = "newer" };
        _dbContext.Message.AddRange(older, newer);
        await _dbContext.SaveChangesAsync();

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(m => m.MessageContent).Should().Equal("older", "newer");
    }

    [Fact]
    public async Task CreateThreadAsync_EmptySubject_ReturnsValidationFailure()
    {
        var user = AddViewer();

        var result = await MakeService().CreateThreadAsync(user.Id, "", "content");

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "subject");
    }

    [Fact]
    public async Task CreateThreadAsync_EmptyContent_ReturnsValidationFailure()
    {
        var user = AddViewer();

        var result = await MakeService().CreateThreadAsync(user.Id, "subject", "");

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "firstMessageContent");
    }

    [Fact]
    public async Task CreateThreadAsync_Valid_CreatesThreadAndFirstMessage()
    {
        var user = AddViewer();

        var result = await MakeService().CreateThreadAsync(user.Id, "My subject", "Hello world");

        result.IsSuccess.Should().BeTrue();
        result.Value.ThreadSubject.Should().Be("My subject");
        _dbContext.MessageThread.Should().ContainSingle(t => t.ThreadSubject == "My subject");
        _dbContext.Message.Should().ContainSingle(m => m.MessageContent == "Hello world");
    }

    [Fact]
    public async Task PostMessageAsync_ViewerForbidden_ReturnsForbidden()
    {
        var user = AddViewer(canViewMessageboard: false);
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, "hi", null, null, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ForbiddenError);
    }

    [Fact]
    public async Task PostMessageAsync_ThreadNotFound_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().PostMessageAsync(Guid.NewGuid(), user.Id, "hi", null, null, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task PostMessageAsync_NoContentImageOrYouTube_ReturnsValidationFailure()
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, null, null, null, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "content");
    }

    [Fact]
    public async Task PostMessageAsync_ValidContent_IncrementsPosterTotalAndNotifies()
    {
        var user = AddViewer(totalPosts: 4);
        var thread = AddThread();
        _avatarService.Setup(a => a.GetAvatarUrl(user.Id, user.ImageUploaded)).Returns("avatar.png");

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, "Hello", null, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.PosterTotalMessageboardPosts.Should().Be(5);
        result.Value.PostedByAvatarUrl.Should().Be("avatar.png");
        _dbContext.Users.Single(u => u.Id == user.Id).TotalMessageboardPosts.Should().Be(5);
        _notifier.Verify(n => n.NotifyNewMessageAsync(thread.MessageThreadID, It.IsAny<MessageModel>(), default), Times.Once);
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public async Task PostMessageAsync_YouTubeUrlFormats_ParsedToVideoId(string input, string expectedVideoId)
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, null, input, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.YouTubeVideoID.Should().Be(expectedVideoId);
    }

    [Fact]
    public async Task AddReactionAsync_MessageNotFound_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().AddReactionAsync(Guid.NewGuid(), user.Id, "u:1f44d", "thumbs up");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task AddReactionAsync_NewReaction_AddsAndNotifies()
    {
        var user = AddViewer();
        var thread = AddThread();
        var message = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow };
        _dbContext.Message.Add(message);
        await _dbContext.SaveChangesAsync();

        var result = await MakeService().AddReactionAsync(message.MessageID, user.Id, "u:1f44d", "thumbs up");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.ReactionId == "u:1f44d" && r.UserID == user.Id);
        _notifier.Verify(n => n.NotifyReactionsChangedAsync(thread.MessageThreadID, message.MessageID, It.IsAny<IReadOnlyList<MessageReactionModel>>(), default), Times.Once);
    }

    [Fact]
    public async Task AddReactionAsync_AlreadyReacted_DoesNotDuplicate()
    {
        var user = AddViewer();
        var thread = AddThread();
        var message = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow };
        _dbContext.Message.Add(message);
        await _dbContext.SaveChangesAsync();
        var service = MakeService();
        await service.AddReactionAsync(message.MessageID, user.Id, "u:1f44d", "thumbs up");

        var result = await service.AddReactionAsync(message.MessageID, user.Id, "u:1f44d", "thumbs up");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveReactionAsync_MessageNotFound_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().RemoveReactionAsync(Guid.NewGuid(), user.Id, "u:1f44d");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task RemoveReactionAsync_ExistingReaction_RemovesIt()
    {
        var user = AddViewer();
        var thread = AddThread();
        var message = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow };
        _dbContext.Message.Add(message);
        await _dbContext.SaveChangesAsync();
        var service = MakeService();
        await service.AddReactionAsync(message.MessageID, user.Id, "u:1f44d", "thumbs up");

        var result = await service.RemoveReactionAsync(message.MessageID, user.Id, "u:1f44d");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkThreadReadAsync_ThreadNotFound_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().MarkThreadReadAsync(Guid.NewGuid(), user.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task MarkThreadReadAsync_FirstTime_InsertsReadRow()
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().MarkThreadReadAsync(thread.MessageThreadID, user.Id);

        result.IsSuccess.Should().BeTrue();
        _dbContext.MessageThreadRead.Should().ContainSingle(r => r.UserID == user.Id && r.MessageThreadID == thread.MessageThreadID);
    }

    [Fact]
    public async Task MarkThreadReadAsync_AlreadyRead_UpdatesLastReadDateTime()
    {
        var user = AddViewer();
        var thread = AddThread();
        var service = MakeService();
        await service.MarkThreadReadAsync(thread.MessageThreadID, user.Id);
        var firstReadAt = _dbContext.MessageThreadRead.Single().LastReadDateTime;
        await Task.Delay(10);

        var result = await service.MarkThreadReadAsync(thread.MessageThreadID, user.Id);

        result.IsSuccess.Should().BeTrue();
        _dbContext.MessageThreadRead.Should().ContainSingle();
        _dbContext.MessageThreadRead.Single().LastReadDateTime.Should().BeAfter(firstReadAt);
    }
}
