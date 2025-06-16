using System;
using Juicebox;

namespace Facepunch.Juicebox;

public class GamePlayer : IEquatable<GamePlayer>
{
	public readonly JuiceboxPlayer JuiceboxPlayer;

	public string Name => JuiceboxPlayer.Name;
	public bool IsConnected => JuiceboxPlayer.IsConnected;
	public int Score { get; set; }
	public string Answer { get; set; }
	public string Vote { get; set; }
	public int VotesReceived { get; set; }

	public GamePlayer( JuiceboxPlayer player )
	{
		JuiceboxPlayer = player ?? throw new ArgumentNullException( nameof(player) );
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

		return JuiceboxPlayer.Equals(other.JuiceboxPlayer);
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
		return JuiceboxPlayer.GetHashCode();
	}

	public static bool operator ==(GamePlayer left, GamePlayer right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(GamePlayer left, GamePlayer right)
	{
		return !Equals(left, right);
	}
}
