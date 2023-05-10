using System.Collections.Generic;
using System.Linq;
using Juicebox;

namespace Facepunch.Juicebox;

public class QuestionPrompt : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.QuestionPrompt;

	public override double? TimeoutSeconds => 60;

	public override void OnEnter()
	{
		base.OnEnter();

		if ( !GameSession.TryTakeQuestion( out var questionEntry ) )
		{
			GameSession.SwitchState( new GameOver() );
			return;
		}

		GameSession.Question = questionEntry.Question;
		GameSession.ImageAnswers = questionEntry.Drawn;

		GameSession.Display( new JuiceboxDisplay
		{
			Header = new JuiceboxHeader { RoundNumber = GameSession.RoundNumber, RoundTime = 60, },
			Stage = new JuiceboxStage
			{
				Title = GameSession.Question,
			},
			Form = new JuiceboxForm
			{
				Controls = new List<JuiceboxFormControl>
				{
					GameSession.ImageAnswers
						? new JuiceboxDrawing { Key = "answer", Width = 320, Height = 240 }
						: new JuiceboxInput { Key = "answer", Label = "Response", Placeholder = "Type your response...", MaxLength = 100 },
				},
			},
		} );

		GameSession.Players.ForEach( p => p.Answer = null );
	}

	public override void OnTimedOut()
	{
		base.OnTimedOut();

		var responseCount = GameSession.Players.Count( p => !string.IsNullOrEmpty( p.Answer ) );
		GameSession.SwitchState( responseCount >= 2 ? new QuestionVote() : new QuestionPrompt() );
	}

	public override void OnPlayerResponse( GamePlayer player, Dictionary<string, string> data )
	{
		base.OnPlayerResponse( player, data );

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

		if ( !string.IsNullOrEmpty( player.Answer ) )
		{
			Log.Warning( $"Received answer from {player.Name}, but they already answered this question" );
			return;
		}

		player.Answer = answer;

		if ( GameSession.Players.All( p => !string.IsNullOrEmpty( p.Answer ) ) )
		{
			GameSession.SwitchState( new QuestionVote() );
		}
	}
}
