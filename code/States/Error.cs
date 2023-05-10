using System;

namespace Facepunch.Juicebox;

public class Error : BaseGameState
{
	public override GameScreen DisplayScreen => GameScreen.Error;

	public override void OnExit()
	{
		throw new Exception( "Cannot leave the error state" );
	}
}
