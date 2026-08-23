using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests.Messageboard;

/// <summary>
/// Exercises IX_MessageReaction_MessageID_UserID_ReactionId, the unique index that guarantees one
/// reaction per user per identity per message. MessageboardService checks before inserting, but
/// that read-then-write is a race under concurrent requests, so the index is what actually
/// enforces it - and an index constraint isn't something EF's InMemory provider models, so it can
/// only be verified against real SQL Server.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MessageReactionUniquenessTests
{
    private readonly DatabaseFixture _fixture;

    public MessageReactionUniquenessTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MessageReaction_SameUserSameIdentityOnOneMessage_IsRejected()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var (user, thread, message) = await CreateMessageAsync(dbContext);

        try
        {
            dbContext.MessageReaction.Add(MakeReaction(message.MessageID, user.Id, "u:2764"));
            await dbContext.SaveChangesAsync();

            // Bypasses the service's own pre-check deliberately - this asserts the database itself
            // refuses the duplicate, which is what makes the service's race harmless.
            dbContext.MessageReaction.Add(MakeReaction(message.MessageID, user.Id, "u:2764"));

            var duplicate = async () => await dbContext.SaveChangesAsync();

            await duplicate.Should().ThrowAsync<Exception>();
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
            await CleanUpAsync(dbContext, message.MessageID, thread.MessageThreadID, user.Id);
        }
    }

    [Fact]
    public async Task MessageReaction_DifferentIdentitiesOrUsers_AreAllowed()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var (user, thread, message) = await CreateMessageAsync(dbContext);
        var otherUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"reactor-{Guid.NewGuid():N}" };
        dbContext.Users.Add(otherUser);
        await dbContext.SaveChangesAsync();

        try
        {
            // The index is on all three columns, so only the exact triple is constrained.
            dbContext.MessageReaction.AddRange(
                MakeReaction(message.MessageID, user.Id, "u:2764"),
                MakeReaction(message.MessageID, user.Id, "c:ludo"),
                MakeReaction(message.MessageID, otherUser.Id, "u:2764"));

            await dbContext.SaveChangesAsync();

            var stored = await dbContext.MessageReaction
                .Where(r => r.MessageID == message.MessageID)
                .CountAsync();

            stored.Should().Be(3);
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
            await CleanUpAsync(dbContext, message.MessageID, thread.MessageThreadID, user.Id, otherUser.Id);
        }
    }

    private static MessageReaction MakeReaction(Guid messageId, Guid userId, string reactionId) => new()
    {
        MessageReactionID = Guid.NewGuid(),
        MessageID = messageId,
        UserID = userId,
        ReactionId = reactionId,
        ReactionName = reactionId,
        CreationDate = DateTime.UtcNow,
    };

    private static async Task<(ApplicationUser User, MessageThread Thread, Message Message)> CreateMessageAsync(ApplicationDbContext dbContext)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"reactor-{Guid.NewGuid():N}" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var thread = new MessageThread
        {
            MessageThreadID = Guid.NewGuid(),
            ThreadSubject = $"Integration Test {Guid.NewGuid():N}",
            StartedByUserID = user.Id,
            StartedDateTime = DateTime.UtcNow,
            HiddenFromPublic = false,
        };
        dbContext.MessageThread.Add(thread);
        await dbContext.SaveChangesAsync();

        var message = new Message
        {
            MessageID = Guid.NewGuid(),
            MessageThreadID = thread.MessageThreadID,
            PostedByUserID = user.Id,
            MessageDateTime = DateTime.UtcNow,
        };
        dbContext.Message.Add(message);
        await dbContext.SaveChangesAsync();

        return (user, thread, message);
    }

    private static async Task CleanUpAsync(ApplicationDbContext dbContext, Guid messageId, Guid threadId, params Guid[] userIds)
    {
        dbContext.MessageReaction.RemoveRange(dbContext.MessageReaction.Where(r => r.MessageID == messageId));
        await dbContext.SaveChangesAsync();

        dbContext.Message.RemoveRange(dbContext.Message.Where(m => m.MessageID == messageId));
        await dbContext.SaveChangesAsync();

        dbContext.MessageThread.RemoveRange(dbContext.MessageThread.Where(t => t.MessageThreadID == threadId));
        dbContext.Users.RemoveRange(dbContext.Users.Where(u => userIds.Contains(u.Id)));
        await dbContext.SaveChangesAsync();
    }
}
