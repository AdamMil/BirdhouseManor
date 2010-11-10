using System;
using System.Diagnostics;
using System.Net;
using GameLib.Network;

namespace BirdhouseManor
{

class Client
{
  public Client()
  {
    client.MessageReceived += client_MessageReceived;
    client.Disconnected    += client_Disconnected;

    foreach(string messageName in Enum.GetNames(typeof(MessageType)))
    {
      client.MessageConverter.RegisterType(Type.GetType("BirdhouseManor." + messageName + "Message"));
    }
  }

  public bool IsConnected
  {
    get { return State != ClientState.Disconnected; }
  }

  public int PlayerId
  {
    get; private set;
  }

  public string PlayerName
  {
    get { return _playerName; }
    set
    {
      if(IsConnected) throw new InvalidOperationException();
      _playerName = value;
    }
  }

  public GameState GameState
  {
    get; private set;
  }

  public void Connect(IPEndPoint endPoint)
  {
    if(string.IsNullOrEmpty(PlayerName)) throw new InvalidOperationException("No player name has been set.");
    Disconnect();
    client.Connect(endPoint);
    State = ClientState.LoggingIn;
    client.Send(new LoginMessage(PlayerName));
  }

  public void Disconnect()
  {
    Disconnect(null);
  }

  public void Disconnect(string reason)
  {
    if(client.IsConnected) client.Send(new DisconnectMessage(PlayerId, reason));
    client.DelayedDisconnect(1000);
  }

  public void ProcessMessages()
  {
    client.Poll();
  }

  public void SendMessage(Message message)
  {
    SendMessage(message, SendFlag.Reliable);
  }

  public void SendMessage(Message message, SendFlag flags)
  {
    client.Send(message, flags);
  }

  enum ClientState
  {
    Disconnected, LoggingIn, InLobby, WaitingForPlayers
  }

  ClientState State
  {
    get; set;
  }

  void client_Disconnected(GameLib.Network.Client client)
  {
    PlayerId  = 0;
    GameState = null;
    State     = ClientState.Disconnected;
  }

  void client_MessageReceived(GameLib.Network.Client client, object msg)
  {
    Message message = msg as Message;
    if(message == null) return;

    switch(message.Type)
    {
      case MessageType.Chat:
      {
        ChatMessage m = (ChatMessage)message;
        // TODO: PlayerData fromPlayer = m.FromPlayer == ChatMessage.FromServer ? null : World.Players.GetById(m.FromPlayer);
        // OnChatMessage(fromPlayer, m.Message, m.TeamChat);
        break;
      }

      case MessageType.Welcome:
      {
        Debug.Assert(State == ClientState.LoggingIn);
        WelcomeMessage m = (WelcomeMessage)message;
        PlayerId = m.PlayerId;
        State    = ClientState.InLobby;
        break;
      }
    }
  }

  readonly GameLib.Network.Client client = new GameLib.Network.Client();
  string _playerName;
}

} // namespace BirdhouseManor