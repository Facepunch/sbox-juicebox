using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;

namespace Juicebox;

public sealed class JuiceboxSession : IDisposable
{
	private readonly CancellationTokenSource _cts;
	private readonly Dictionary<string, JuiceboxPlayer> _players;
	private readonly Dictionary<string, JuiceboxDisplay> _playerDisplays;
	private JuiceboxDisplay _defaultDisplay;
	private long _sessionId;
	private string _secretKey;
	private WebSocket _webSocket;

	public string JoinPassword { get; private set; }

	public delegate void ResponseReceivedEvent( JuiceboxSession session, JuiceboxPlayer player, Dictionary<string, string> data );
	public event ResponseReceivedEvent OnResponseReceived;

	public delegate void ActionReceivedEvent( JuiceboxSession session, JuiceboxPlayer player, string key );
	public event ActionReceivedEvent OnActionReceived;

	public delegate void SessionClosedEvent( JuiceboxSession session );
	public event SessionClosedEvent OnSessionClosed;

	public bool IsOpen { get; private set; }

	public bool IsConnected => IsOpen && _webSocket != null && _webSocket.IsConnected;

	public IReadOnlyCollection<JuiceboxPlayer> Players => _players.Values;

	public JuiceboxSession()
	{
		_cts = new CancellationTokenSource();
		_players = new Dictionary<string, JuiceboxPlayer>();
		_playerDisplays = new Dictionary<string, JuiceboxDisplay>();
	}

	~JuiceboxSession() => Dispose( false );

	public void Dispose() => Dispose( true );

	private void Dispose( bool disposing )
	{
		if ( _sessionId > 0 && !string.IsNullOrWhiteSpace( _secretKey ) && IsOpen )
		{
			try
			{
				JuiceboxWebApi.SessionDestroy( _sessionId, _secretKey ).GetAwaiter().GetResult();
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}

        if ( !_cts.IsCancellationRequested )
        {
            _cts.Cancel();
            _cts.Dispose();
        }

		_players.Clear();
		_playerDisplays.Clear();
		_defaultDisplay = null;
		_sessionId = 0;
		_secretKey = null;
		JoinPassword = null;
		IsOpen = false;
		_webSocket?.Dispose();
		_webSocket = null;
	}

	public async Task Start()
	{
		if ( _cts.IsCancellationRequested )
		{
			throw new ObjectDisposedException( nameof( JuiceboxSession ) );
		}

		if ( IsOpen )
		{
			throw new InvalidOperationException( "A session has already been started." );
		}

		var createResponse = await JuiceboxWebApi.SessionCreate();
		_sessionId = createResponse.SessionId;
		_secretKey = createResponse.HostSecretKey;
		JoinPassword = createResponse.JoinPassword;
		IsOpen = true;

		var thisRef = new WeakReference<JuiceboxSession>( this );
		PingLoop( thisRef, _cts.Token );
	}

	public async ValueTask Display( JuiceboxDisplay display, JuiceboxPlayer forPlayer = null )
	{
		if ( _cts.IsCancellationRequested )
		{
			throw new ObjectDisposedException( nameof( JuiceboxSession ) );
		}

		if ( !IsOpen )
		{
			throw new InvalidOperationException( "A session has not been started yet." );
		}

		if ( display == null )
		{
			throw new ArgumentNullException( nameof( display ) );
		}

		if ( forPlayer != null && forPlayer.SessionId != _sessionId )
		{
			throw new ArgumentException( "Target player does not belong to this session.", nameof( forPlayer ) );
		}

		await SendMessage( new DisplayRequest { Display = display }, forPlayer?.Name );

		if ( forPlayer == null )
		{
			_playerDisplays.Clear();
			_defaultDisplay = display;
		}
		else
		{
			_playerDisplays[forPlayer.Name] = display;
		}
	}

	private async Task Connect()
	{
		_webSocket?.Dispose();
		_webSocket = null;

		JuiceboxWebApi.SessionNegotiateResponse negotiateResponse;
		try
		{
			negotiateResponse = await JuiceboxWebApi.SessionNegotiate( _sessionId, _secretKey );
		}
		catch ( HttpRequestException e ) when ( e.StatusCode == HttpStatusCode.NotFound )
		{
			if ( IsOpen )
			{
				IsOpen = false;
				OnSessionClosed?.Invoke( this );
			}

			return;
		}
		catch ( Exception e )
		{
			Log.Error( e, "Failed to negotiate connection to the Juicebox session" );
			return;
		}

		try
		{
			_webSocket = new WebSocket();
			await _webSocket.Connect( negotiateResponse.Endpoint );
			Log.Info( $"Connected to Juicebox session websocket" );
			_webSocket.OnDisconnected += ( status, reason ) =>
            {
                if (reason != "Disposing" && reason != "Disposed")
                {
                    Log.Error($"Lost connection to the Juicebox session websocket ({status}, {reason})");
                }
            };
			_webSocket.OnMessageReceived += HandleWebSocketMessage;
		}
		catch ( Exception e )
		{
			Log.Error( e, $"Failed to connect to the Juicebox session websocket ({negotiateResponse.Endpoint})" );
		}
	}

	private async ValueTask SendMessage<T>( T message, string to = null, CancellationToken ct = default )
	{
		while ( !ct.IsCancellationRequested && IsOpen )
		{
			if ( _webSocket != null && _webSocket.IsConnected )
			{
				await _webSocket.Send( JsonSerializer.Serialize( new SendMessageFrame<T> { To = to, Message = message } ) );
				return;
			}

			await Task.Delay( 1000, ct );
		}
	}

	private void HandleWebSocketMessage( string message )
	{
		if ( message == "ack" || message.StartsWith( "fail:" ) )
		{
			return;
		}

		try
		{
			var obj = JsonSerializer.Deserialize<JsonObject>( message );
			if ( obj == null || !obj.TryGetPropertyValue( "Type", out var typeNode ) || typeNode == null )
			{
				Log.Warning( $"Ignoring badly formatted message: {message}" );
				return;
			}

			var type = typeNode.GetValue<string>();
			switch ( type )
			{
				case "Connected":
					var connectedEvent = obj.Deserialize<ConnectedControlFrame>();
					if ( connectedEvent != null )
					{
						GetOrAddPlayer( connectedEvent.MemberName ).IsConnected = true;

						if ( _playerDisplays.TryGetValue( connectedEvent.MemberName, out var playerDisplay ) )
						{
							SendMessage( new DisplayRequest { Display = playerDisplay }, connectedEvent.MemberName );
						}
						else if ( _defaultDisplay != null )
						{
							SendMessage( new DisplayRequest { Display = _defaultDisplay }, connectedEvent.MemberName );
						}
					}
					break;
				case "Disconnected":
					var disconnectedEvent = obj.Deserialize<DisconnectedControlFrame>();
					if ( disconnectedEvent != null )
					{
						GetOrAddPlayer( disconnectedEvent.MemberName ).IsConnected = false;
					}
					break;
				case "Message":
					var messageEvent = obj.Deserialize<MessageControlFrame>();
					if ( messageEvent?.Message != null && !string.IsNullOrEmpty( messageEvent.MemberName ) )
					{
						GetOrAddPlayer( messageEvent.MemberName ).IsConnected = true;
						HandleClientMessage( messageEvent.MemberName, messageEvent.Message );
					}
					break;
				default:
					Log.Warning( $"Unhandled WebSocket message type: {type}" );
					break;
			}
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}

	private void HandleClientMessage( string fromName, JsonObject messageObject )
	{
		if ( !messageObject.TryGetPropertyValue( "Type", out var messageTypeNode ) || messageTypeNode == null )
		{
			return;
		}

		var player = GetOrAddPlayer( fromName );
		player.IsConnected = true;

		var type = messageTypeNode.GetValue<string>();
		switch ( type )
		{
			case "Response":
				var responseMessage = messageObject.Deserialize<DisplayResponse>();
				if ( responseMessage?.Fields != null )
				{
					OnResponseReceived?.Invoke( this, player, responseMessage.Fields );
				}
				break;
			case "Action":
				var actionMessage = messageObject.Deserialize<ActionResponse>();
				if ( !string.IsNullOrEmpty( actionMessage?.Key ) )
				{
					OnActionReceived?.Invoke( this, player, actionMessage.Key );
				}
				break;
			default:
				Log.Error( $"Unhandled client message type: {type}" );
				break;
		}
	}

	private static async void PingLoop( WeakReference<JuiceboxSession> sessionRef, CancellationToken ct )
	{
		while ( !ct.IsCancellationRequested )
		{
			if ( !sessionRef.TryGetTarget( out var session ) || !session.IsOpen )
			{
				return;
			}

			if ( !session.IsConnected )
			{
				Log.Info( "Connecting to Juicebox session websocket..." );
				await session.Connect();
			}

			try
			{
				var pingResponse = await JuiceboxWebApi.SessionPing( session._sessionId, session._secretKey );
				foreach ( var name in pingResponse.MemberNames )
				{
					session.GetOrAddPlayer( name );
				}
			}
			catch ( HttpRequestException e ) when ( e.StatusCode == HttpStatusCode.NotFound )
			{
				if ( session.IsOpen )
				{
					session.IsOpen = false;
					session.OnSessionClosed?.Invoke( session );
				}

				return;
			}
			catch ( Exception e )
			{
				Log.Error( e );
				throw;
			}

			await Task.Delay( 10_000, ct );
		}
	}

	private JuiceboxPlayer GetOrAddPlayer( string name )
	{
		if ( !_players.TryGetValue( name, out var player ) )
		{
			player = new JuiceboxPlayer( _sessionId, name );
			_players.Add( name, player );
		}

		return player;
	}
}
