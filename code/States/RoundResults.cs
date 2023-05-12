using Juicebox;

namespace Facepunch.Juicebox;

public class RoundResults : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.Results;

	public override double? TimeoutSeconds => 10;

	public override void OnExit()
	{
		base.OnExit();

		GameSession.Display( new JuiceboxDisplay
		{
			Header = new JuiceboxHeader
			{
				RoundNumber = GameSession.RoundNumber,
			},
		} );

		GameSession.RoundNumber++;
	}

	public override void OnTimedOut()
	{
		base.OnTimedOut();

		GameSession.SwitchState( new QuestionPrompt() );
	}
}
