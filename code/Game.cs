using Sandbox;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

	[Event.Tick.Client]
	public static void ServerTick()
	{
		GameState.Update();
	}

	[ConCmd.Client( "tts" )]
	public static async void Tts( string msg )
	{
		await Say( msg );
	}

	public static async Task Say( string msg )
	{
		Log.Info( $"Saying: {msg}" );

		if ( string.IsNullOrWhiteSpace( msg ) )
		{
			return;
		}

		using var http = new Sandbox.Internal.Http( new Uri( $"http://localhost:5208/tts?msg={WebUtility.UrlEncode( msg )}" ) );
		var bytes = await http.GetBytesAsync();
		var numChannels = BitConverter.ToInt16( bytes, 22 );
		var sampleRate = BitConverter.ToInt32( bytes, 24 );
		var stream = Sound.FromScreen( "tts" ).CreateStream( sampleRate, numChannels );
		var samples = new short[(bytes.Length - 44) / sizeof( short )];
		for ( var i = 0; i < samples.Length; i++ )
		{
			samples[i] = BitConverter.ToInt16( bytes, 44 + i * sizeof( short ) );
		}
		stream.WriteData( samples );
		while ( stream.QueuedSampleCount > 0 )
		{
			await GameTask.Yield();
		}
		stream.Delete();
	}
}
