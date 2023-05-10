using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Facepunch.Juicebox;

public class QuestionList
{
	[JsonPropertyName( "questions" )]
	public List<QuestionEntry> Questions { get; set; }
}

public class QuestionEntry
{
	[JsonPropertyName("question")]
	public string Question { get; set; }

	[JsonPropertyName("drawn")]
	public bool Drawn { get; set; }
}
