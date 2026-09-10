using EkbReviews.Domain;

namespace EkbReviews.Application;

public sealed class ReviewConversationExtractor
{
    public ReviewConversation Extract(Review review)
    {
        var messages = new List<ConversationMessage>
        {
            new(
                ConversationMessageAuthorType.Customer,
                review.AuthorName,
                review.Text.Trim(),
                review.PublishedAt)
        };

        messages.AddRange(review.Replies
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .OrderBy(x => x.PublishedAt)
            .Select(x => new ConversationMessage(
                ConversationMessageAuthorType.Business,
                x.AuthorName,
                x.Text.Trim(),
                x.PublishedAt)));

        return new ReviewConversation(review.Id, messages);
    }
}
