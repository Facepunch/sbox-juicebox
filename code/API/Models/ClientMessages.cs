using System.Collections.Generic;

namespace Juicebox;

internal class DisplayRequest
{
	public string Type => "Display";
	public JuiceboxDisplay Display { get; set; }
}

internal class DisplayResponse
{
	public string Type => "Response";
	public Dictionary<string, string> Fields { get; set; }
}

internal class ActionResponse
{
	public string Type => "Action";
	public string Key { get; set; }
}
