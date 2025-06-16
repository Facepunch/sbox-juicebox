using Juicebox;

namespace Facepunch.Juicebox;

public class QuestionSetUp : WaitingForPlayers
{
	public QuestionSetUp() : base( false )
	{
	}

	public override GameScreen DisplayScreen => GameScreen.SettingUp;

	protected override void UpdateDisplay()
	{
		GameSession.Display( new JuiceboxDisplay
		{
			Stage = new JuiceboxStage
			{
				Title = "Session is still being set up...",
			},
		} );
	}
}
