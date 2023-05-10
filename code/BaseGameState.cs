using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Facepunch.Juicebox;

public abstract class BaseGameState
{
	private readonly Stopwatch _activeTimer = new Stopwatch();
	private bool _timedOut;

	public abstract GameScreen DisplayScreen { get; }

	public double ActiveSeconds => _activeTimer.Elapsed.TotalSeconds;

	public double? RemainingSeconds => TimeoutSeconds != null ? Math.Max( TimeoutSeconds.Value - ActiveSeconds, 0 ) : null;

	public virtual double? TimeoutSeconds => null;

	public virtual void OnEnter()
	{
		_activeTimer.Restart();
	}

	public virtual void OnExit()
	{
		_activeTimer.Stop();
	}

	public virtual void OnTimedOut() { }

	public virtual void OnPlayerJoin( GamePlayer player ) { }

	public virtual void OnPlayerResponse( GamePlayer player, Dictionary<string, string> data ) { }

	public virtual void OnPlayerAction( GamePlayer player, string actionKey ) { }

	public virtual void Update()
	{
		if ( !_timedOut && RemainingSeconds <= 0 )
		{
			_timedOut = true;
			OnTimedOut();
		}
	}
}
