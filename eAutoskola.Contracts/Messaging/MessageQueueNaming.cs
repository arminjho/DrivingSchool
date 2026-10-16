using System.Text;

namespace eAutoskola.Contracts.Messaging;

/// <summary>
/// Single source of truth for message-type -> queue-name mapping, shared by the Infrastructure
/// publisher and the Worker's consumers so they can never drift apart (they did once, during
/// the generalization from the single-purpose IEmailPublisher to IMessagePublisher - fixed by
/// extracting this here instead of duplicating the naming logic in both projects).
/// </summary>
public static class MessageQueueNaming
{
    public static string GetQueueName<TMessage>() => GetQueueName(typeof(TMessage).Name);

    /// <summary>"LessonConfirmedMessage" -> "lesson_confirmed".</summary>
    public static string GetQueueName(string typeName)
    {
        const string suffix = "Message";
        var name = typeName.EndsWith(suffix, StringComparison.Ordinal) ? typeName[..^suffix.Length] : typeName;

        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                builder.Append('_');
            }
            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}
