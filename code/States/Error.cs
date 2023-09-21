using System;

namespace Facepunch.Juicebox;

public class Error : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.Error;

	public override void OnExit()
	{
		Log.Warning( "Leaving the error state!" );
	}
}
