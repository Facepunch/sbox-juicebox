using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Juicebox;

public partial class Juicebox : Game
{
	public override void ClientSpawn()
	{
		Local.Hud?.Delete( true );
		Local.Hud = new UI.Hud();

		base.ClientSpawn();
	}

	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );
		
		var pawn = new Pawn();
		client.Pawn = pawn;

		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}

	[Event.Tick.Server]
	public static void ServerTick()
	{
		GameState.Update();
	}
}
