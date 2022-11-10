using Sandbox.UI;

namespace Sandbox;

public partial class Hud : HudEntity<RootPanel>
{
	public Hud()
	{
		if ( IsClient )
		{
			RootPanel.StyleSheet.Load( "/UI/Styles/juicebox.scss" );
		}
	}
}
