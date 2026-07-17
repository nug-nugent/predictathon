#nullable enable

using System;

namespace Predictathon.Domain.Entities;

public partial class MessageThreadRead
{
    public int MessageThreadReadID { get; set; }

    public Guid UserID { get; set; }

    public Guid MessageThreadID { get; set; }

    public DateTime LastReadDateTime { get; set; }

    public virtual MessageThread MessageThread { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
