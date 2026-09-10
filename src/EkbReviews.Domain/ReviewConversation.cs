namespace EkbReviews.Domain;

public enum ConversationMessageAuthorType
{
    Customer,
    Business,
    Unknown
}

public sealed record ConversationMessage(
    ConversationMessageAuthorType AuthorType,
    string AuthorName,
    string Text,
    DateTimeOffset PublishedAt);

public sealed record ReviewConversation(
    string ReviewId,
    IReadOnlyList<ConversationMessage> Messages)
{
    public int MessageCount => Messages.Count;
    public bool HasBusinessReply => Messages.Any(x => x.AuthorType == ConversationMessageAuthorType.Business);
    public bool IsDialogue => Messages.Count >= 2 && HasBusinessReply;
}
