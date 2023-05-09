using System;
using Juicebox;

namespace Facepunch.Juicebox;

public class GamePlayer : IEquatable<GamePlayer>
{
	private readonly JuiceboxPlayer _player;

	public string Name => _player.Name;
	public bool IsConnected => _player.IsConnected;
	public int Score { get; set; }
	public string Answer { get; set; }
	public string Vote { get; set; }
	public int VotesReceived { get; set; }

	public GamePlayer( JuiceboxPlayer player )
	{
		_player = player ?? throw new ArgumentNullException( nameof(player) );
	}

	public bool Equals(GamePlayer other)
	{
		if (ReferenceEquals(null, other))
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return _player.Equals(other._player);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != this.GetType())
		{
			return false;
		}

		return Equals((GamePlayer)obj);
	}

	public override int GetHashCode()
	{
		return _player.GetHashCode();
	}
}
