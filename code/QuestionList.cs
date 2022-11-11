using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Facepunch.Juicebox;
public class QuestionList
{
	[JsonPropertyName( "questions" )]
	public List<string> Questions { get; set; }
}
