using System;
using System.Collections.Generic;
using System.Linq;
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

			CurrentScreen = GameScreen.QuestionPrompt;
			return;
		}

		Log.Warning( $"Unknown action from {player.Name}: {key}" );
	}
}
