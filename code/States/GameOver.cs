﻿using Juicebox;

namespace Facepunch.Juicebox;

public class GameOver : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.Results;

	public override void OnEnter()
	{
		base.OnEnter();

		GameSession.Display( new JuiceboxDisplay
		{
			Stage = new JuiceboxStage
			{
				Title = "Game Over",
			},
		} );
	}
}
