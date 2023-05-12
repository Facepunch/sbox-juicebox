using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Juicebox;

namespace Facepunch.Juicebox;

public enum GameScreen
{
	SettingUp, WaitingForPlayers, QuestionPrompt, Voting, Results, GameOver, Error,
}

public static class GameSession
{
	public static GameScreen CurrentScreen => _currentState?.DisplayScreen ?? GameScreen.SettingUp;

	public static string JoinCode => _session?.JoinPassword ?? "";

	public static int RoundNumber { get; set; } = 1;

	public static string RoundTimer
	{
		get
		{
			var remaining = _currentState?.RemainingSeconds;
			if ( remaining == null )
			{
				return "00:00";
			}

			var remainingTime = TimeSpan.FromSeconds( remaining.Value );
			return $"{remainingTime.Minutes:00}:{remainingTime.Seconds:00}";
		}
	}

	public static List<GamePlayer> Players { get; set; } = new List<GamePlayer>();

	public static GamePlayer HostPlayer { get; set; } = null;

	public static bool ImageAnswers { get; set; } = false;

	public static string Question { get; set; } = "The worst Halloween costume for a young child";

	private static JuiceboxSession _session;
	private static bool _sessionStarted;
	private static Queue<QuestionEntry> _questions;
	private static BaseGameState _currentState;

	private static async void Initialize()
	{
		try
		{
			var questionList = FileSystem.Mounted.ReadJson<QuestionList>( "questions.json" );
			var questions = (questionList?.Questions ?? new List<QuestionEntry>()).OrderBy( _ => Guid.NewGuid() ).Take( 20 );
			_questions = new Queue<QuestionEntry>( questions );

			await _session.Start();
			_session.OnActionReceived += SessionActionReceived;
			_session.OnResponseReceived += SessionResponseReceived;
			_sessionStarted = true;
			SwitchState( new WaitingForPlayers() );
		}
		catch ( Exception e )
		{
			SwitchState( new Error() );
			Log.Error( e );
		}
	}

	public static void Shutdown()
	{
		SwitchState( new GameOver() );
		_session?.Dispose();
	}

	public static void Update()
	{
		if ( _session == null )
		{
			_session = new JuiceboxSession();
			Initialize();
		}

		if ( CurrentScreen == GameScreen.Error )
		{
			return;
		}

		if ( _sessionStarted && (_session == null || !_session.IsOpen) )
		{
			SwitchState( new Error() );
		}

		RegisterNewPlayers();

		_currentState?.Update();
	}

	public static async void Display( JuiceboxDisplay display, GamePlayer forPlayer = null )
	{
		if ( _session == null )
		{
			return;
		}

		if ( display.Header?.RoundTime != null )
		{
			display.Header.RoundTime = (int)(display.Header.RoundTime * BaseGameState.TimeoutScale);
		}

		await _session.Display( display, forPlayer?.JuiceboxPlayer );
	}

	public static void SwitchState( BaseGameState newState )
	{
		if ( newState == null )
		{
			throw new ArgumentNullException( nameof( newState ) );
		}

		var oldState = _currentState;
		_currentState?.OnExit();

		// if OnExit switched state then we shouldn't enter this state anymore
		if ( _currentState == oldState )
		{
			_currentState = newState;
			_currentState.OnEnter();
		}
	}

	public static GamePlayer FindPlayer( string name )
	{
		return Players.Find( p => string.Equals( p.Name, name, StringComparison.InvariantCultureIgnoreCase ) );
	}

	public static bool TryTakeQuestion( out QuestionEntry question )
	{
		return _questions.TryDequeue( out question );
	}

	private static void RegisterNewPlayers()
	{
		var changed = false;
		foreach ( var player in _session.Players )
		{
			var index = Players.FindIndex( gp => gp.Name == player.Name );
			if ( index < 0 )
			{
				var gamePlayer = new GamePlayer( player );
				Players.Add( gamePlayer );
				changed = true;

				_currentState?.OnPlayerJoin( gamePlayer );
			}
		}

		if ( changed )
		{
			Players = Players
				.OrderByDescending( p => p.Score )
				.ThenBy( p => p.Name, StringComparer.InvariantCultureIgnoreCase )
				.ToList();
		}
	}

	private static void SessionResponseReceived( JuiceboxSession session, JuiceboxPlayer player, Dictionary<string, string> data )
	{
		var gamePlayer = FindPlayer( player.Name );
		if ( gamePlayer == null )
		{
			Log.Warning( $"Received response from {player.Name}, but they have no GamePlayer!" );
			return;
		}

		_currentState?.OnPlayerResponse( gamePlayer, data );
	}

	private static void SessionActionReceived( JuiceboxSession session, JuiceboxPlayer player, string key )
	{
		var gamePlayer = FindPlayer( player.Name );
		if ( gamePlayer == null )
		{
			Log.Warning( $"Received action from {player.Name}, but they have no GamePlayer!" );
			return;
		}

		_currentState?.OnPlayerAction( gamePlayer, key );
	}
}
