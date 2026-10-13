namespace ArenaStorm.WebAPI.Entities;

/// <summary>
/// Represents a chat message sent within a game lobby.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the chat message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the foreign key referencing the user who sent the message.
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the sender.
    /// </summary>
    public User? Sender { get; set; }

    /// <summary>
    /// Gets or sets the lobby identifier where the message was posted.
    /// </summary>
    public string LobbyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text content of the chat message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
