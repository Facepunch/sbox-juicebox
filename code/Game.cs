using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Juicebox;

public partial class Juicebox : GameManager
{
	public override void ClientSpawn()
	{
		Game.RootPanel?.Delete( true );
		Game.RootPanel = new UI.Hud();

		base.ClientSpawn();
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		var pawn = new Pawn();
		client.Pawn = pawn;

		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		var randomSpawnPoint = spawnpoints.MinBy( x => Guid.NewGuid() );

		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}

	[GameEvent.Tick.Client]
	public static void ServerTick()
	{
		GameState.Update();
	}
}
