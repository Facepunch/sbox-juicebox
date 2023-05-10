using System;
using System.Collections.Generic;
using System.Linq;
using Juicebox;

namespace Facepunch.Juicebox;

public class QuestionVote : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.Voting;

	public override double? TimeoutSeconds => 60;

	public override void OnEnter()
	{
		base.OnEnter();

		GameSession.Display( new JuiceboxDisplay
		{
			Header = new JuiceboxHeader { RoundNumber = GameSession.RoundNumber, RoundTime = 60 },
			Stage = new JuiceboxStage
			{
				Title = GameSession.Question,
			},
			Form = new JuiceboxForm
			{
				Controls = new List<JuiceboxFormControl>
				{
					new JuiceboxRadioGroup
					{
						Key = "vote",
						Options = GameSession.Players
							.Where( p => !string.IsNullOrEmpty( p.Answer ) )
							.Select( p => new JuiceboxRadioOption { Label = p.Answer, Value = p.Name } )
							.OrderBy( _ => Guid.NewGuid() )
							.ToList(),
					},
				},
			},
		} );

		GameSession.Players.ForEach( p =>
		{
			p.Vote = null;
			p.VotesReceived = 0;
		} );
	}

	public override void OnExit()
	{
		base.OnExit();

		foreach ( var player in GameSession.Players )
		{
			var votedFor = GameSession.FindPlayer( player.Vote );
			if ( votedFor != null )
			{
				votedFor.VotesReceived++;
				votedFor.Score++;
			}
		}
	}

	public override void OnTimedOut()
	{
		base.OnTimedOut();

		var voteCount = GameSession.Players.Count( p => !string.IsNullOrEmpty( p.Vote ) );
		GameSession.SwitchState( voteCount > 0 ? new RoundResults() : new QuestionPrompt() );
	}

	public override void OnPlayerResponse( GamePlayer player, Dictionary<string, string> data )
	{
		base.OnPlayerResponse( player, data );

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

		if ( !string.IsNullOrEmpty( player.Vote ) )
		{
			Log.Warning( $"Received vote from {player.Name}, but they already voted" );
			return;
		}

		player.Vote = vote;

		if ( GameSession.Players.All( p => !string.IsNullOrEmpty( p.Vote ) ) )
		{
			GameSession.SwitchState( new RoundResults() );
		}
	}
}
