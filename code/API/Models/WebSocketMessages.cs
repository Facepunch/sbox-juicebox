using System.Text.Json.Nodes;

namespace Juicebox;

internal class ConnectedControlFrame
{
	public string Type => "Connected";
	public string MemberName { get; set; }
}

internal class DisconnectedControlFrame
{
	public string Type => "Disconnected";
	public string MemberName { get; set; }
}

internal class MessageControlFrame
{
	public string Type => "Message";
	public string MemberName { get; set; }
	public JsonObject Message { get; set; }
}

internal class SendMessageFrame<T>
{
	public string To { get; set; }
	public T Message { get; set; }
}
