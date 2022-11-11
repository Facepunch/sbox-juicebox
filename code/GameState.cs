using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Juicebox;

namespace Facepunch.Juicebox;

public enum GameScreen
{
	SettingUp, WaitingForPlayers, QuestionPrompt, Voting, Results,
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
	private static TaskCompletionSource _receivedAllVotes;
	private static Queue<string> _questions;

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
				if ( _questions == null || _questions.Count == 0 )
				{
					return;
				}

				Question = _questions.Dequeue();

				CurrentScreen = GameScreen.QuestionPrompt;
				_session.Display( new JuiceboxDisplay
				{
					RoundHeader = new JuiceboxHeader { RoundNumber = RoundNumber, RoundTime = 60, },
					Question = Question,
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

				Players.ForEach( p => p.Answer = null );
				_receivedAllAnswers = new TaskCompletionSource();
				await GameTask.WhenAny( GameTask.Delay( 60000 ), _receivedAllAnswers.Task );
				
				var receivedAnyAnswers = Players.Any( p => !string.IsNullOrEmpty( p.Answer ) );
				if ( receivedAnyAnswers )
				{
					CurrentScreen = GameScreen.Voting;
					_session.Display( new JuiceboxDisplay
					{
						RoundHeader = new JuiceboxHeader { RoundNumber = RoundNumber, RoundTime = 60 },
						Question = Question,
						Answer = new JuiceboxAnswer
						{
							Form = new List<JuiceboxFormControl>
							{
								new JuiceboxRadioGroup
								{
									Key = "vote",
									Label = "Answers",
									Options = Players
										.Where( p => !string.IsNullOrEmpty( p.Answer ) )
										.Select( p => new JuiceboxRadioOption { Label = p.Answer, Value = p.Name } )
										.OrderBy( _ => Guid.NewGuid() )
										.ToList(),
								},
							},
						},
					} );

					Players.ForEach( p =>
					{
						p.Vote = null;
						p.VotesReceived = 0;
					} );
					_receivedAllVotes = new TaskCompletionSource();
					await GameTask.WhenAny( GameTask.Delay( 60000 ), _receivedAllVotes.Task );

					foreach ( var player in Players )
					{
						var votedFor = FindPlayer( player.Vote );
						if ( votedFor != null )
						{
							votedFor.VotesReceived++;
							votedFor.Score++;
						}
					}

					var bestAnswer = Players
						.OrderByDescending( p => p.VotesReceived )
						.Select( p => p.Answer )
						.FirstOrDefault();
					if ( bestAnswer != null )
					{
						SpeakWinningAnswer( Question, bestAnswer );
					}

					CurrentScreen = GameScreen.Results;
					_session.Display( new JuiceboxDisplay
					{
						RoundHeader = new JuiceboxHeader { RoundNumber = RoundNumber },
					} );

					await Task.Delay( 15000 );
				}

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

	private static readonly Regex PromptPattern = new Regex( @"^(.*?)(_+)(.*?)$" );
	private static async void SpeakWinningAnswer( string question, string answer )
	{
		try
		{
			var match = PromptPattern.Match( question );
			if ( match.Success )
			{
				await Juicebox.Say( match.Groups[1] + answer + match.Groups[3] );
			}
			else
			{
				await Juicebox.Say( question + " " + answer );
			}
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}

	private static GamePlayer FindPlayer( string name )
	{
		return Players.Find( p => string.Equals( p.Name, name, StringComparison.InvariantCultureIgnoreCase ) );
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
			var questionList = FileSystem.Mounted.ReadJson<QuestionList>( "questions.json" );
			var questions = (questionList?.Questions ?? new List<string>()).OrderBy( _ => Guid.NewGuid() ).Take( 20 );
			_questions = new Queue<string>( questions );

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
		var gamePlayer = FindPlayer( player.Name );
		if ( gamePlayer == null )
		{
			Log.Warning( $"Received response from {player.Name}, but they have no GamePlayer!" );
			return;
		}

		if ( CurrentScreen == GameScreen.QuestionPrompt )
		{
			if ( data == null || !data.TryGetValue( "answer", out var answer ) )
			{
				Log.Warning( $"Received answer from {player.Name}, but it's missing the answer key" );
				return;
			}

			if ( string.IsNullOrWhiteSpace( answer ) )
			{
				Log.Warning( $"Received answer from {player.Name}, but it's blank" );
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

			return;
		}

		if ( CurrentScreen == GameScreen.Voting )
		{
			if ( data == null || !data.TryGetValue( "vote", out var vote ) )
			{
				Log.Warning( $"Received vote from {player.Name}, but it's missing the vote key" );
				return;
			}

			if ( string.IsNullOrWhiteSpace( vote ) )
			{
				Log.Warning( $"Received vote from {player.Name}, but it's blank" );
				return;
			}

			if ( !string.IsNullOrEmpty( gamePlayer.Vote ) )
			{
				Log.Warning( $"Received vote from {player.Name}, but they already voted" );
				return;
			}

			gamePlayer.Vote = vote;

			if ( Players.All( p => !string.IsNullOrEmpty( p.Vote ) ) )
			{
				_receivedAllVotes?.TrySetResult();
			}

			return;
		}

		Log.Warning( $"Received response from {player.Name}, but don't know what to do with it in state {CurrentScreen}" );
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
