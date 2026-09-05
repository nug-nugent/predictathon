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
    private readonly Mock<ITrophyService> _trophyService = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManager.Create();

    public MessageboardServiceTests()
    {
        // Every identity resolves to an image and is already canonical unless a test says
        // otherwise - the catalogue's own resolution and canonicalisation rules are covered by
        // ReactionCatalogueTests.
        _reactionCatalogue.Setup(c => c.ResolveImageFile(It.IsAny<string>())).Returns("1f44d.svg");
        _reactionCatalogue.Setup(c => c.Canonicalise(It.IsAny<string>())).Returns((string id) => id);

        // Trophies are their own feature with their own tests - nobody here has won anything.
        _trophyService.Setup(t => t.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _trophyService.Setup(t => t.GetForUsersAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<UserTrophyModel>>());
    }

    private MessageboardService MakeService()
        => new(_dbContext, _avatarService.Object, _messageImageService.Object, _notifier.Object, _reactionCatalogue.Object, _trophyService.Object, _userManager.Object);

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

    private DomainEntities.Message AddMessage(
        DomainEntities.MessageThread thread,
        ApplicationUser postedBy,
        string? content,
        DateTime? postedAt = null,
        DomainEntities.Message? replyTo = null,
        bool hasLinkedImage = false,
        string? youTubeVideoId = null)
    {
        var message = new DomainEntities.Message
        {
            MessageID = Guid.NewGuid(),
            MessageThreadID = thread.MessageThreadID,
            PostedByUserID = postedBy.Id,
            MessageDateTime = postedAt ?? DateTime.UtcNow.AddMinutes(-1),
            MessageContent = content,
            HasLinkedImage = hasLinkedImage,
            YouTubeVideoID = youTubeVideoId,
            ReplyToMessageID = replyTo?.MessageID,
        };
        _dbContext.Message.Add(message);
        _dbContext.SaveChanges();
        return message;
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

        var result = await MakeService().GetMessagesAsync(Guid.NewGuid(), user.Id, 20, null, null);

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

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Select(m => m.MessageContent).Should().Equal("older", "newer");
    }

    /// <summary>
    /// Fills a thread with sequentially-timed messages named "Message 1".."Message n", oldest
    /// first, so paging assertions can talk about positions rather than juggling ids.
    /// </summary>
    /// <param name="thread">The thread to fill.</param>
    /// <param name="postedBy">The author of every message.</param>
    /// <param name="count">How many messages to create.</param>
    private List<DomainEntities.Message> AddMessages(DomainEntities.MessageThread thread, ApplicationUser postedBy, int count)
    {
        var start = DateTime.UtcNow.AddMinutes(-count);
        var created = new List<DomainEntities.Message>();

        for (var i = 1; i <= count; i++)
        {
            created.Add(AddMessage(thread, postedBy, $"Message {i}", start.AddMinutes(i)));
        }

        return created;
    }

    private void MarkRead(DomainEntities.MessageThread thread, ApplicationUser user, DateTime lastRead)
    {
        _dbContext.MessageThreadRead.Add(new DomainEntities.MessageThreadRead
        {
            UserID = user.Id,
            MessageThreadID = thread.MessageThreadID,
            LastReadDateTime = lastRead,
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetMessagesAsync_NoCursorAndNeverRead_ReturnsTheNewestPage()
    {
        var user = AddViewer();
        var thread = AddThread();
        AddMessages(thread, user, 45);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().HaveCount(30);
        result.Value.Messages[0].MessageContent.Should().Be("Message 16");
        result.Value.Messages[^1].MessageContent.Should().Be("Message 45");
        result.Value.MessagesBefore.Should().Be(15);
        result.Value.MessagesAfter.Should().Be(0);
        // Nothing to resume from, so no boundary is claimed - a first-time reader of a long thread
        // gets the newest page, not the top of it.
        result.Value.FirstUnreadMessageID.Should().BeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_NoCursorAndFullyCaughtUp_ReturnsTheNewestPageWithNoUnreadBoundary()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);
        MarkRead(thread, user, messages[^1].MessageDateTime);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages[^1].MessageContent.Should().Be("Message 45");
        result.Value.MessagesAfter.Should().Be(0);
        result.Value.FirstUnreadMessageID.Should().BeNull();
    }

    // The point of the whole thing: come back to a long thread and it opens where you stopped, not
    // at the end - with the last message you did read still on screen above the boundary.
    [Fact]
    public async Task GetMessagesAsync_NoCursorWithUnreadMessages_AnchorsOneMessageBeforeTheFirstUnread()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);
        // Read up to and including Message 10, so Message 11 is the first unread.
        MarkRead(thread, user, messages[9].MessageDateTime);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstUnreadMessageID.Should().Be(messages[10].MessageID);
        result.Value.Messages[0].MessageContent.Should().Be("Message 10");
        result.Value.Messages[1].MessageContent.Should().Be("Message 11");
        result.Value.MessagesBefore.Should().Be(9);
        // 45 total, 9 skipped, 30 returned - six still ahead of the reader.
        result.Value.MessagesAfter.Should().Be(6);
    }

    // The anchor only ever moves the window earlier. With one unread message the boundary is deep
    // in the last page already, so anchoring on it must not push the window off the end.
    [Fact]
    public async Task GetMessagesAsync_UnreadMessageInsideTheLastPage_StillReturnsTheNewestPage()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);
        MarkRead(thread, user, messages[43].MessageDateTime);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstUnreadMessageID.Should().Be(messages[44].MessageID);
        result.Value.Messages[0].MessageContent.Should().Be("Message 16");
        result.Value.MessagesBefore.Should().Be(15);
        result.Value.MessagesAfter.Should().Be(0);
    }

    [Fact]
    public async Task GetMessagesAsync_ShorterThanOnePage_ReturnsEverythingWithNoCountsEitherSide()
    {
        var user = AddViewer();
        var thread = AddThread();
        AddMessages(thread, user, 5);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().HaveCount(5);
        result.Value.MessagesBefore.Should().Be(0);
        result.Value.MessagesAfter.Should().Be(0);
    }

    [Fact]
    public async Task GetMessagesAsync_BeforeCursor_FillsBackwardsFromIt()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);

        // Paging back from Message 16, the start of the newest page.
        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 10, messages[15].MessageID, null);

        result.IsSuccess.Should().BeTrue();
        // The ten immediately older, not the ten oldest.
        result.Value.Messages[0].MessageContent.Should().Be("Message 6");
        result.Value.Messages[^1].MessageContent.Should().Be("Message 15");
        result.Value.MessagesBefore.Should().Be(5);
    }

    [Fact]
    public async Task GetMessagesAsync_BeforeCursorNearTheStart_ReturnsWhatIsLeftAndNothingBefore()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 10, messages[3].MessageID, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Select(m => m.MessageContent).Should().Equal("Message 1", "Message 2", "Message 3");
        result.Value.MessagesBefore.Should().Be(0);
    }

    [Fact]
    public async Task GetMessagesAsync_AfterCursor_FillsForwardsFromIt()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 10, null, messages[9].MessageID);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages[0].MessageContent.Should().Be("Message 11");
        result.Value.Messages[^1].MessageContent.Should().Be("Message 20");
        result.Value.MessagesAfter.Should().Be(25);
    }

    [Fact]
    public async Task GetMessagesAsync_AfterCursorReachingTheEnd_ReportsNothingNewer()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 45);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 30, null, messages[39].MessageID);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().HaveCount(5);
        result.Value.Messages[^1].MessageContent.Should().Be("Message 45");
        result.Value.MessagesAfter.Should().Be(0);
    }

    [Fact]
    public async Task GetMessagesAsync_BothCursors_ReturnsValidationFailure()
    {
        var user = AddViewer();
        var thread = AddThread();
        var messages = AddMessages(thread, user, 5);

        var result = await MakeService().GetMessagesAsync(
            thread.MessageThreadID, user.Id, 10, messages[3].MessageID, messages[1].MessageID);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "beforeMessageId");
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

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, "hi", null, null, null, replyToMessageId: null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ForbiddenError);
    }

    [Fact]
    public async Task PostMessageAsync_ThreadNotFound_ReturnsNotFound()
    {
        var user = AddViewer();

        var result = await MakeService().PostMessageAsync(Guid.NewGuid(), user.Id, "hi", null, null, null, replyToMessageId: null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task PostMessageAsync_NoContentImageOrYouTube_ReturnsValidationFailure()
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, null, null, null, null, replyToMessageId: null);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "content");
    }

    [Fact]
    public async Task PostMessageAsync_ValidContent_IncrementsPosterTotalAndNotifies()
    {
        var user = AddViewer(totalPosts: 4);
        var thread = AddThread();
        _avatarService.Setup(a => a.GetAvatarUrl(user.Id, user.ImageUploaded)).Returns("avatar.png");

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, "Hello", null, null, null, replyToMessageId: null);

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

        var result = await MakeService().PostMessageAsync(thread.MessageThreadID, user.Id, null, input, null, null, replyToMessageId: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.YouTubeVideoID.Should().Be(expectedVideoId);
    }

    [Fact]
    public async Task PostMessageAsync_ReplyToMessageInSameThread_StoresParentAndReturnsStub()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, "The parent post");

        var result = await MakeService().PostMessageAsync(
            thread.MessageThreadID, user.Id, "Replying to that", null, null, null, replyToMessageId: parent.MessageID);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReplyTo.Should().NotBeNull();
        result.Value.ReplyTo!.MessageID.Should().Be(parent.MessageID);
        result.Value.ReplyTo.PostedByUsername.Should().Be("viewer");
        result.Value.ReplyTo.Snippet.Should().Be("The parent post");
        _dbContext.Message.Single(m => m.MessageContent == "Replying to that").ReplyToMessageID.Should().Be(parent.MessageID);
    }

    [Fact]
    public async Task PostMessageAsync_ReplyToUnknownMessage_ReturnsValidationFailure()
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(
            thread.MessageThreadID, user.Id, "hi", null, null, null, replyToMessageId: Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "replyToMessageId");
        _dbContext.Message.Should().BeEmpty();
    }

    // The constraint that actually matters for visibility: thread access is checked per thread, so
    // a reply that could quote a message from a different (possibly hidden) thread would carry
    // content past that check.
    [Fact]
    public async Task PostMessageAsync_ReplyToMessageInAnotherThread_ReturnsValidationFailure()
    {
        var user = AddViewer();
        var thread = AddThread();
        var otherThread = AddThread();
        var parent = AddMessage(otherThread, user, "Somewhere else entirely");

        var result = await MakeService().PostMessageAsync(
            thread.MessageThreadID, user.Id, "hi", null, null, null, replyToMessageId: parent.MessageID);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.PropertyName == "replyToMessageId");
        _dbContext.Message.Should().ContainSingle();
    }

    [Fact]
    public async Task PostMessageAsync_NoReplyTo_LeavesStubNull()
    {
        var user = AddViewer();
        var thread = AddThread();

        var result = await MakeService().PostMessageAsync(
            thread.MessageThreadID, user.Id, "Just a post", null, null, null, replyToMessageId: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReplyTo.Should().BeNull();
        _dbContext.Message.Single().ReplyToMessageID.Should().BeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_ReplyWithParentOnThePage_PopulatesStub()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, "The parent post", DateTime.UtcNow.AddMinutes(-5));
        AddMessage(thread, user, "The reply", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().HaveCount(2);
        result.Value.Messages[0].ReplyTo.Should().BeNull();
        result.Value.Messages[1].ReplyTo.Should().NotBeNull();
        result.Value.Messages[1].ReplyTo!.MessageID.Should().Be(parent.MessageID);
        result.Value.Messages[1].ReplyTo!.Snippet.Should().Be("The parent post");
    }

    // The case the denormalised stub exists for: the parent is older than the window being
    // returned, so nothing the client receives could resolve it from the page itself.
    [Fact]
    public async Task GetMessagesAsync_ReplyWithParentOutsideThePage_StillPopulatesStub()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, "Long ago", DateTime.UtcNow.AddMinutes(-30));
        AddMessage(thread, user, "Filler", DateTime.UtcNow.AddMinutes(-20));
        AddMessage(thread, user, "The reply", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 1, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().ContainSingle();
        result.Value.Messages[0].MessageContent.Should().Be("The reply");
        result.Value.Messages[0].ReplyTo.Should().NotBeNull();
        result.Value.Messages[0].ReplyTo!.MessageID.Should().Be(parent.MessageID);
        result.Value.Messages[0].ReplyTo!.Snippet.Should().Be("Long ago");
    }

    [Fact]
    public async Task GetMessagesAsync_ReplyToImageOnlyParent_StubCarriesTheImageAndNoSnippet()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, content: null, hasLinkedImage: true);
        _messageImageService.Setup(s => s.GetImageUrl(parent.MessageID, true)).Returns("parent.jpg");
        AddMessage(thread, user, "Look at that", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages[1].ReplyTo!.Snippet.Should().BeNull();
        result.Value.Messages[1].ReplyTo!.ImageUrl.Should().Be("parent.jpg");
        result.Value.Messages[1].ReplyTo!.HasYouTubeVideo.Should().BeFalse();
    }

    [Fact]
    public async Task GetMessagesAsync_ReplyToYouTubeOnlyParent_StubFlagsTheVideo()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, content: null, youTubeVideoId: "dQw4w9WgXcQ");
        AddMessage(thread, user, "Great tune", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Messages[1].ReplyTo!.HasYouTubeVideo.Should().BeTrue();
        result.Value.Messages[1].ReplyTo!.Snippet.Should().BeNull();
    }

    [Fact]
    public async Task GetMessagesAsync_ReplyToLongParent_TruncatesSnippetAtAWordBoundary()
    {
        var user = AddViewer();
        var thread = AddThread();
        // 40 four-character words - comfortably past the 120-character snippet limit, with spaces
        // falling regularly enough that the break must land on one.
        var parent = AddMessage(thread, user, string.Join(' ', Enumerable.Repeat("word", 40)));
        AddMessage(thread, user, "Shorter", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        var snippet = result.Value.Messages[1].ReplyTo!.Snippet!;
        snippet.Should().EndWith("…");
        snippet.Should().StartWith("word word");
        snippet.TrimEnd('…').Should().EndWith("word");
        snippet.Length.Should().BeLessThanOrEqualTo(121);
    }

    [Fact]
    public async Task GetMessagesAsync_ReplyToMultiLineParent_CollapsesSnippetOntoOneLine()
    {
        var user = AddViewer();
        var thread = AddThread();
        var parent = AddMessage(thread, user, "First line\r\n\r\nSecond line");
        AddMessage(thread, user, "Reply", DateTime.UtcNow, replyTo: parent);

        var result = await MakeService().GetMessagesAsync(thread.MessageThreadID, user.Id, 20, null, null);

        result.Value.Messages[1].ReplyTo!.Snippet.Should().Be("First line Second line");
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
    public async Task AddReactionAsync_SameEmojiUnderTwoSpellings_StoresOneReaction()
    {
        // Production carried both the legacy site's spelling of the red heart and the picker's,
        // which showed as two pills that couldn't toggle each other. Both must reduce to one row.
        _reactionCatalogue.Setup(c => c.Canonicalise("u:2764-fe0f")).Returns("u:2764");
        _reactionCatalogue.Setup(c => c.Canonicalise("u:2764")).Returns("u:2764");

        var user = AddViewer();
        var thread = AddThread();
        var message = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow };
        _dbContext.Message.Add(message);
        await _dbContext.SaveChangesAsync();
        var service = MakeService();
        await service.AddReactionAsync(message.MessageID, user.Id, "u:2764", "Red Heart");

        var result = await service.AddReactionAsync(message.MessageID, user.Id, "u:2764-fe0f", "Red Heart");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.ReactionId == "u:2764");
    }

    [Fact]
    public async Task RemoveReactionAsync_NonCanonicalSpelling_RemovesTheStoredReaction()
    {
        // A client holding the picker's spelling must still be able to remove a row stored under
        // the canonical one, or reactions become impossible to un-react.
        _reactionCatalogue.Setup(c => c.Canonicalise("u:2764-fe0f")).Returns("u:2764");
        _reactionCatalogue.Setup(c => c.Canonicalise("u:2764")).Returns("u:2764");

        var user = AddViewer();
        var thread = AddThread();
        var message = new DomainEntities.Message { MessageID = Guid.NewGuid(), MessageThreadID = thread.MessageThreadID, PostedByUserID = user.Id, MessageDateTime = DateTime.UtcNow };
        _dbContext.Message.Add(message);
        await _dbContext.SaveChangesAsync();
        var service = MakeService();
        await service.AddReactionAsync(message.MessageID, user.Id, "u:2764", "Red Heart");

        var result = await service.RemoveReactionAsync(message.MessageID, user.Id, "u:2764-fe0f");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
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
