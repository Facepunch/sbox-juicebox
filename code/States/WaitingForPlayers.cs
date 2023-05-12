using System.Collections.Generic;
using Juicebox;

namespace Facepunch.Juicebox;

public class WaitingForPlayers : BaseGameState
{
	private readonly bool _canStartGame;

	public WaitingForPlayers() : this( true )
	{
	}

	protected WaitingForPlayers( bool canStartGame = true )
	{
		_canStartGame = canStartGame;
	}

	public override GameScreen DisplayScreen => GameScreen.WaitingForPlayers;

	public override void OnEnter()
	{
		base.OnEnter();

		UpdateDisplay();
	}

	public override void OnPlayerJoin( GamePlayer player )
	{
		base.OnPlayerJoin( player );

		GameSession.HostPlayer ??= player;

		UpdateDisplay();
	}

	// TODO: check if the host has left, then promote someone else to host

	public override void OnPlayerAction( GamePlayer player, string actionKey )
	{
		base.OnPlayerAction( player, actionKey );

		if ( _canStartGame && player == GameSession.HostPlayer && actionKey == "start_game" )
		{
			GameSession.SwitchState( new QuestionPrompt() );
		}
	}

	protected virtual void UpdateDisplay()
	{
		if ( GameSession.Players.Count == 0 )
		{
			GameSession.Display( new JuiceboxDisplay
			{
				Stage = new JuiceboxStage
				{
					Title = "Waiting for players...",
				},
			} );
		}
		else
		{

			foreach ( var gamePlayer in GameSession.Players )
			{
				if ( gamePlayer == GameSession.HostPlayer )
				{
					if ( GameSession.Players.Count < 2 )
					{
						GameSession.Display( new JuiceboxDisplay
						{
							Stage = new JuiceboxStage
							{
								Title = "You are the host!\nRequires minimum 2 players.",
							},
						}, gamePlayer );
					}
					else
					{
						GameSession.Display( new JuiceboxDisplay
						{
							Stage = new JuiceboxStage
							{
								Title = "Is everybody ready?",
							},
							Form = new JuiceboxForm
							{
								Controls = new List<JuiceboxFormControl>
								{
									new JuiceboxButton
									{
										Key = "start_game",
										Label = "Start Game",
									},
								},
							},
						}, gamePlayer );
					}
				}
				else
				{
					GameSession.Display( new JuiceboxDisplay
					{
						Stage = new JuiceboxStage
						{
							Title = $"Waiting for {GameSession.HostPlayer.Name} to start the game...",
						},
					}, gamePlayer );
				}
			}
		}
	}
}
