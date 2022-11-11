using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Juicebox;

namespace Facepunch.Juicebox;

public enum GameScreen
{
	SettingUp, WaitingForPlayers, QuestionPrompt, QuestionResults,
}

public static class GameState
{
	public static GameScreen CurrentScreen { get; private set; } = GameScreen.WaitingForPlayers;

	public static string JoinCode => _session?.JoinPassword ?? "";

	public static int RoundNumber { get; private set; } = 1;

	public static List<GamePlayer> Players { get; private set; } = new List<GamePlayer>();

	public static JuiceboxPlayer HostPlayer { get; private set; } = null;

	public static string Question { get; private set; } = "The worst Halloween costume for a young child";

	private static JuiceboxSession _session;
	private static TaskCompletionSource _receivedAllAnswers;

	public static void Update()
	{
		if ( _session == null )
		{
			_session = new JuiceboxSession();
			Initialize();
		}

		if ( _session == null || !_session.IsOpen )
		{
			return;
		}

		RegisterNewPlayers();
	}

	private static async void QuestionLoop()
	{
		try
		{
			while ( true )
			{
				CurrentScreen = GameScreen.QuestionPrompt;
				_session.Display( new JuiceboxDisplay
				{
					RoundHeader = new JuiceboxHeader { RoundNumber = RoundNumber, RoundTime = 60, },
					Question = $"(Round {RoundNumber}) The worst Halloween costume for a young child",
					Answer = new JuiceboxAnswer
					{
						Form = new List<JuiceboxFormControl>
						{
							new JuiceboxInput
							{
								Key = "answer",
								Label = "Answer",
								MaxLength = 100,
								Placeholder = "Type your answer...",
								Value = "",
							},
						},
					},
				} );

				_receivedAllAnswers = new TaskCompletionSource();
				Players.ForEach( p => p.Answer = null );
				await GameTask.WhenAny( GameTask.Delay( 60000 ), _receivedAllAnswers.Task );

				CurrentScreen = GameScreen.QuestionResults;
				_session.Display( new JuiceboxDisplay
				{
					RoundHeader = new JuiceboxHeader { RoundNumber = RoundNumber },
				} );

				await Task.Delay( 10000 );
				RoundNumber++;

			}
		}
		catch ( OperationCanceledException )
		{
			// ignore
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
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
				HostPlayer ??= player;
				changed = true;
			}
		}

		if ( changed )
		{
			Players = Players
				.OrderByDescending( p => p.Score )
				.ThenBy( p => p.Name, StringComparer.InvariantCultureIgnoreCase )
				.ToList();

			if ( CurrentScreen == GameScreen.WaitingForPlayers )
			{
				if ( Players.Count < 2 )
				{
					_session.Display( new JuiceboxDisplay
					{
						Question = "You are the host!\nRequires minimum 2 players.",
					}, HostPlayer );
				}
				else
				{
					_session.Display( new JuiceboxDisplay
					{
						Question = "Is everybody ready?",
						Answer = new JuiceboxAnswer
						{
							Form = new List<JuiceboxFormControl>
							{
								new JuiceboxButton
								{
									Key = "start_game",
									Label = "Start Game",
								},
							},
						},
					}, HostPlayer );
				}
			}

		}
	}

	private static async void Initialize()
	{
		try
		{
			await _session.Start();
			_session.OnActionReceived += SessionActionReceived;
			_session.OnResponseReceived += SessionResponseReceived;
			CurrentScreen = GameScreen.WaitingForPlayers;

			_session.Display( new JuiceboxDisplay
			{
				Question = "Waiting for players..."
			} );
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}

	private static void SessionResponseReceived( JuiceboxSession session, JuiceboxPlayer player, Dictionary<string, string> data )
	{
		var gamePlayer = Players.Find( p => p.Name == player.Name );
		if ( gamePlayer == null )
		{
			Log.Warning( $"Received response from {player.Name}, but they have no GamePlayer!" );
			return;
		}

		if ( data == null || !data.TryGetValue( "answer", out var answer ) )
		{
			Log.Warning( $"Received response from {player.Name}, but it's missing the answer key" );
			return;
		}

		if ( string.IsNullOrWhiteSpace( answer ) )
		{
			Log.Warning( $"Received response from {player.Name}, but it's blank" );
			return;
		}

		if ( !string.IsNullOrEmpty( gamePlayer.Answer ) )
		{
			Log.Warning( $"Received answer from {player.Name}, but they already answered this question" );
			return;
		}

		gamePlayer.Answer = answer;

		if ( Players.All( p => !string.IsNullOrEmpty( p.Answer ) ) )
		{
			_receivedAllAnswers?.TrySetResult();
		}
	}

	private static void SessionActionReceived( JuiceboxSession session, JuiceboxPlayer player, string key )
	{
		if ( key == "start_game" )
		{
			if ( player != HostPlayer )
			{
				Log.Error( $"{player.Name} tried to start the game, but they aren't the host" );
				return;
			}

			if ( CurrentScreen != GameScreen.WaitingForPlayers )
			{
				Log.Error( $"{player.Name} tried to start the game but it's already started" );
				return;
			}

			QuestionLoop();
			return;
		}

		Log.Warning( $"Unknown action from {player.Name}: {key}" );
	}
}
