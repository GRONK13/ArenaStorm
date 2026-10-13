using MessagePack;

namespace ArenaStorm.Shared.Protocol;

/// <summary>
/// Provides serialization and deserialization utilities for ArenaStorm network packets.
/// Packets are prefixed with a 1-byte <see cref="MessageType"/> header followed by a MessagePack payload.
/// </summary>
public static class PacketSerializer
{
    /// <summary>
    /// Serializes a message object with a 1-byte <see cref="MessageType"/> header prefix.
    /// </summary>
    /// <typeparam name="T">Type of message payload to serialize.</typeparam>
    /// <param name="type">Network message type identifier.</param>
    /// <param name="message">Payload message instance.</param>
    /// <returns>A byte array containing the 1-byte header followed by the MessagePack-encoded payload.</returns>
    public static byte[] Serialize<T>(MessageType type, T message)
    {
        byte[] payload = MessagePackSerializer.Serialize(message);
        byte[] packet = new byte[payload.Length + 1];
        packet[0] = (byte)type;
        Buffer.BlockCopy(payload, 0, packet, 1, payload.Length);
        return packet;
    }

    /// <summary>
    /// Reads the 1-byte <see cref="MessageType"/> header from raw packet data and returns the payload slice.
    /// </summary>
    /// <param name="data">Raw received packet bytes.</param>
    /// <param name="type">Output message type extracted from the header.</param>
    /// <returns>A <see cref="ReadOnlyMemory{Byte}"/> view of the remaining payload bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when data is null or empty.</exception>
    public static ReadOnlyMemory<byte> Deserialize(byte[] data, out MessageType type)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Packet data cannot be null or empty.", nameof(data));
        }

        type = (MessageType)data[0];
        return new ReadOnlyMemory<byte>(data, 1, data.Length - 1);
    }

    /// <summary>
    /// Deserializes a payload memory segment into the specified message type.
    /// </summary>
    /// <typeparam name="T">Target message type.</typeparam>
    /// <param name="payload">Payload byte memory slice.</param>
    /// <returns>The deserialized message instance.</returns>
    public static T DeserializePayload<T>(ReadOnlyMemory<byte> payload)
    {
        return MessagePackSerializer.Deserialize<T>(payload);
    }
}
