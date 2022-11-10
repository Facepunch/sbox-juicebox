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
				changed = true;
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

	private static async void Initialize()
	{
		try
		{
			await _session.Start();
			CurrentScreen = GameScreen.WaitingForPlayers;
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}
}
