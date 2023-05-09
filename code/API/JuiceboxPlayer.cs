using System;

namespace Juicebox;

public sealed class JuiceboxPlayer : IEquatable<JuiceboxPlayer>
{
	internal long SessionId { get; }
	public string Name { get; }
	public DateTimeOffset Joined { get; }
	public bool IsConnected { get; internal set; }

	internal JuiceboxPlayer( long sessionId, string memberName )
	{
		SessionId = sessionId;
		Name = memberName ?? throw new ArgumentNullException( nameof( memberName ) );
		Joined = DateTimeOffset.UtcNow;
	}

	public bool Equals(JuiceboxPlayer other)
	{
		if (ReferenceEquals(null, other))
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return SessionId == other.SessionId && string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
	}

	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || obj is JuiceboxPlayer other && Equals(other);
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(SessionId);
		hashCode.Add(Name, StringComparer.InvariantCultureIgnoreCase);
		return hashCode.ToHashCode();
	}

	public static bool operator ==(JuiceboxPlayer left, JuiceboxPlayer right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(JuiceboxPlayer left, JuiceboxPlayer right)
	{
		return !Equals(left, right);
	}
}
