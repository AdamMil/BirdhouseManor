using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using GameLib.Network;

namespace BirdhouseManor
{

#region PlayerData
sealed class PlayerData
{
  public PlayerData(string name, int id)
  {
    this.Name    = name;
    this.id      = id;
  }

  public int Id
  {
    get { return id; }
  }

  public string Name
  {
    get { return name; }
    set
    {
      if(string.IsNullOrEmpty(value)) throw new ArgumentException();
      name = value;
    }
  }

  string name;
  readonly int id;
}
#endregion

class Server
{
  const int MinimumVersion = (0<<8) | 1; // 0.1

  public Server()
  {
    myVersion = typeof(Server).Assembly.GetName().Version;
    State = ServerState.Offline;

    server.MessageReceived    += server_MessageReceived;
    server.PlayerDisconnected += server_PlayerDisconnected;

    foreach(string messageName in Enum.GetNames(typeof(MessageType)))
    {
      server.MessageConverter.RegisterType(Type.GetType("BirdhouseManor." + messageName + "Message"));
    }
  }

  public IPEndPoint LocalEndPoint
  {
    get { return server.LocalEndPoint; }
  }

  public void Deinitialize()
  {
    server.Deinitialize();
    State = ServerState.Offline;
  }

  public void Initialize(IPEndPoint endPoint)
  {
    Deinitialize();
    server.Listen(endPoint);
    State = ServerState.InLobby;
  }

  public void StopListening()
  {
    server.StopListening();
  }

  enum ServerState
  {
    Offline, InLobby, WaitingForPlayers, InGame
  }

  ServerState State
  {
    get { return state; }
    set
    {
      if(value != State)
      {
        state = value;
      }
    }
  }

  void DisconnectClient(ServerPlayer player, string disconnectMessage)
  {
    server.Send(player, new DisconnectMessage(player.Id, disconnectMessage));
    server.DropPlayerDelayed(player, 1000);
  }

  void HandleLoginMessage(ServerPlayer player, LoginMessage m)
  {
    if(m.Version < MinimumVersion || m.MajorVersion > myVersion.Major ||
       m.MajorVersion == myVersion.Major && m.MinorVersion > myVersion.Minor)
    {
      string disconnectMessage = 
        string.Format(CultureInfo.InvariantCulture,
                      "Your client version ({0}.{1}) is incompatible with the server version ({2}.{3}).",
                      m.MajorVersion, m.MinorVersion, myVersion.Major, myVersion.Minor);
      DisconnectClient(player, disconnectMessage);
    }
    else
    {
      PlayerData data = new PlayerData(m.Name, player.Id);
      player.Data = data;
      lock(playersById) playersById[player.Id] = player;
      server.Send(player, new WelcomeMessage(player.Id));
      server.SendToAllExcept(player, new JoinMessage(data.Id, data.Name));
    }
  }

  void server_MessageReceived(GameLib.Network.Server sender, ServerPlayer player, object msg)
  {
    Message message = msg as Message;
    if(message == null) return;

    PlayerData data = (PlayerData)player.Data;
    switch(message.Type)
    {
      case MessageType.Login:
        HandleLoginMessage(player, (LoginMessage)message);
        break;
    }
  }

  void server_PlayerDisconnected(GameLib.Network.Server server, ServerPlayer player)
  {
    lock(playersById) playersById.Remove(player.Id);
    // if the player was logged in, let everyone know that they (were) disconnected
    if(player.Data != null) server.SendToAll(new DisconnectMessage(player.Id, null));
  }

  readonly GameLib.Network.Server server = new GameLib.Network.Server();
  readonly Dictionary<int, ServerPlayer> playersById = new Dictionary<int, ServerPlayer>();
  readonly Version myVersion;
  ServerState state;
}

} // namespace BirdhouseManor