using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AdamMil.IO;
using GameLib.Network;
using BinaryReader = AdamMil.IO.BinaryReader;
using BinaryWriter = AdamMil.IO.BinaryWriter;

namespace BirdhouseManor
{

/* -- Client protocol:
 *
 * The client can be in any of the following states. After the communication channel is opened, the client begins in the
 * LoggingIn state.
 *   LoggingIn, InLobby, InGame
 *
 * -- Bidirectional messages:
 *
 * -- Client to server messages:
 *
 * -- Server to client messages:
 *
 */

#region MessageType
enum MessageType : byte
{
  Unknown=0,

  // initial version
  Chat, Disconnect, SelectHero, VoiceChat, // bidirectional
  GameRequest, Login, // client to server
  Game, GameState, Join, Welcome, // server to client
}
#endregion

#region Message
[StructLayout(LayoutKind.Sequential, Pack=1)]
abstract class Message
{
  protected Message(MessageType type)
  {
    Type = type;
  }

  public readonly MessageType Type;
}
#endregion

#region SimpleMessage
[StructLayout(LayoutKind.Sequential, Pack=1)]
sealed class SimpleMessage : Message
{
  public SimpleMessage() : base(MessageType.Unknown) { }

  public SimpleMessage(MessageType type) : base(type) { }
}
#endregion

#region Bidirectional messages
#region ChatMessage
sealed class ChatMessage : Message, INetSerializable
{
  public const int FromServer = -1;

  public ChatMessage() : base(MessageType.Chat) { }

  public ChatMessage(int fromPlayerId, string message) : this()
  {
    if(string.IsNullOrEmpty(message)) throw new ArgumentException();
    FromPlayer = fromPlayerId;
    Message    = message;
  }

  public string Message { get; private set; }
  public int FromPlayer { get; private set; }

  #region INetSerializable Members
  void INetSerializable.Serialize(BinaryWriter writer, out Stream attachedStream)
  {
    attachedStream = null;
    writer.WriteEncoded(FromPlayer);
    writer.WriteStringWithLength(Message);
  }

  void INetSerializable.Deserialize(BinaryReader reader, Stream attachedStream)
  {
    FromPlayer = reader.ReadEncodedInt32();
    Message    = reader.ReadStringWithLength();
  }
  #endregion
}
#endregion

#region DisconnectMessage
sealed class DisconnectMessage : Message, INetSerializable
{
  public DisconnectMessage() : base(MessageType.Disconnect) { }

  public DisconnectMessage(int playerId, string reason) : this()
  {
    if(reason == null) throw new ArgumentNullException();
    Player = playerId;
    Reason = reason;
  }

  public string Reason { get; private set; }
  public int Player { get; private set; }

  void INetSerializable.Serialize(BinaryWriter writer, out Stream attachedStream)
  {
    attachedStream = null;
    writer.WriteEncoded(Player);
    writer.WriteStringWithLength(Reason);
  }

  void INetSerializable.Deserialize(BinaryReader reader, Stream attachedStream)
  {
    Player = reader.ReadEncodedInt32();
    Reason = reader.ReadStringWithLength();
  }
}
#endregion

#region SelectHeroMessage
[StructLayout(LayoutKind.Sequential, Pack=1)]
sealed class SelectHeroMessage : Message
{
  const int NoHero = -1;

  public SelectHeroMessage(int playerId, int heroIndex) : base(MessageType.SelectHero)
  {
    Player = playerId;
    Hero   = heroIndex;
  }

  public readonly int Hero, Player;
}
#endregion
#endregion

#region Client to server messages
#region LoginMessage
sealed class LoginMessage : Message, INetSerializable
{
  public LoginMessage(string playerName) : base(MessageType.Login)
  {
    Version version = typeof(LoginMessage).Assembly.GetName().Version;
    Version = ((byte)version.Major << 8) | (byte)version.Minor;
  }

  public int MajorVersion
  {
    get { return Version >> 8; }
  }

  public int MinorVersion
  {
    get { return (byte)Version; }
  }

  public string Name { get; private set; }
  public int Version { get; private set; }

  void INetSerializable.Serialize(BinaryWriter writer, out Stream attachedStream)
  {
    attachedStream = null;
    writer.WriteStringWithLength(Name);
    writer.Write(Version);
  }

  void INetSerializable.Deserialize(BinaryReader reader, Stream attachedStream)
  {
    Name    = reader.ReadStringWithLength();
    Version = reader.ReadInt32();
  }
}
#endregion
#endregion

#region Server to client messages
#region JoinMessage
sealed class JoinMessage : Message, INetSerializable
{
  public JoinMessage() : base(MessageType.Join) { }

  public JoinMessage(int playerId, string playerName) : this()
  {
    if(string.IsNullOrEmpty(playerName)) throw new ArgumentException();
    PlayerId = playerId;
    Name     = playerName;
  }

  public string Name { get; private set; }
  public int PlayerId { get; private set; }

  void INetSerializable.Serialize(BinaryWriter writer, out Stream attachedStream)
  {
    attachedStream = null;
    writer.WriteStringWithLength(Name);
    writer.WriteEncoded(PlayerId);
  }

  void INetSerializable.Deserialize(BinaryReader reader, Stream attachedStream)
  {
    Name     = reader.ReadStringWithLength();
    PlayerId = reader.ReadEncodedInt32();
  }
}
#endregion

#region WelcomeMessage
[StructLayout(LayoutKind.Sequential, Pack=1)]
sealed class WelcomeMessage : Message
{
  public WelcomeMessage() : base(MessageType.Welcome) { }

  public WelcomeMessage(int playerId) : this()
  {
    PlayerId = playerId;
  }

  /// <summary>The ID of the new player as assigned by the server.</summary>
  public readonly int PlayerId;
}
#endregion
#endregion

} // namespace BirdhouseManor