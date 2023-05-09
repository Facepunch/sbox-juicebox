using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace Juicebox;

internal static class JuiceboxWebApi
{
	private const string apiEndpoint = "https://rohan-pubsubhub.loca.lt/api/sessions";
	//private const string apiEndpoint = "https://api.facepunch.com/api/sessions";

	public class SessionCreateResponse
	{
		public long SessionId { get; set; }
		public string HostSecretKey { get; set; }
		public string JoinPassword { get; set; }
	}

	public static async Task<SessionCreateResponse> SessionCreate()
	{
		using var response = await PostAsJson( $"{apiEndpoint}/create", new
		{
			PublicKey = "3ytkDl6H9qyOY6TxLRRSF2xFEvrxsxvJH5RYBlaVLFEQdNB1CvOVXbT1zVTNVgsG",
		} );
		response.EnsureSuccessStatusCode();
		return await ReadFromJson<SessionCreateResponse>( response );
	}

	public class SessionNegotiateResponse
	{
		public string Endpoint { get; set; }
	}

	public static async Task<SessionNegotiateResponse> SessionNegotiate( long sessionId, string hostSecretKey )
	{
		using var response = await PostAsJson( $"{apiEndpoint}/negotiate", new
		{
			SessionId = sessionId,
			HostSecretKey = hostSecretKey,
		} );
		response.EnsureSuccessStatusCode();
		return await ReadFromJson<SessionNegotiateResponse>( response );
	}

	public class SessionPingResponse
	{
		public List<string> MemberNames { get; set; }
	}

	public static async Task<SessionPingResponse> SessionPing( long sessionId, string hostSecretKey )
	{
		using var response = await PostAsJson( $"{apiEndpoint}/ping", new
		{
			SessionId = sessionId,
			HostSecretKey = hostSecretKey,
		} );
		response.EnsureSuccessStatusCode();
		return await ReadFromJson<SessionPingResponse>( response );
	}

	public static async Task SessionDestroy( long sessionId, string hostSecretKey )
	{
		using var response = await PostAsJson( $"{apiEndpoint}/destroy", new
		{
			SessionId = sessionId,
			HostSecretKey = hostSecretKey,
		} );
		response.EnsureSuccessStatusCode();
	}

	private static Task<HttpResponseMessage> PostAsJson<T>( string uri, T payload )
	{
		var content = Http.CreateJsonContent( payload );
		return Http.RequestAsync( "POST", uri, content );
	}

	private static async Task<T> ReadFromJson<T>( HttpResponseMessage response )
	{
		var json = await response.Content.ReadAsStreamAsync();
		return JsonSerializer.Deserialize<T>( json );
	}
}
