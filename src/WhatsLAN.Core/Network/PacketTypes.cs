namespace WhatsLAN.Core.Network;

public enum PacketType : byte
{
    Handshake = 0x01,
    Msg = 0x02,
    AckDelivered = 0x03,
    AckRead = 0x04,
    Typing = 0x05,
    Edit = 0x06,
    Delete = 0x07,
    React = 0x08,
    FileOffer = 0x09,
    FileAccept = 0x0A,
    FileReject = 0x0B,
    FileChunk = 0x0C,
    FileDone = 0x0D,
    ProfileUpdate = 0x0E,
    AvatarRequest = 0x0F,
    AvatarResponse = 0x10,
    GroupCreate = 0x11,
    GroupUpdate = 0x12,
    GroupLeave = 0x13,
    CallInvite = 0x14,
    CallAccept = 0x15,
    CallReject = 0x16,
    CallEnd = 0x17,
    Ping = 0x18,
    Pong = 0x19,
    Bye = 0x1A
}

[Flags]
public enum FrameFlags : byte
{
    None = 0x00,
    Compressed = 0x01,
    Encrypted = 0x02
}
