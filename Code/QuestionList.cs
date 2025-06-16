using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Facepunch.Juicebox;

public class QuestionList
{
	[JsonPropertyName( "title" )]
	public string Title { get; set; }

	[JsonPropertyName( "description" )]
	public string Description { get; set; }

	[JsonPropertyName( "questions" )]
	public List<QuestionEntry> Questions { get; set; }
}

public class QuestionEntry
{
	[JsonPropertyName( "question" )]
	public string Question { get; set; }

	[JsonPropertyName( "mature" )]
	public bool Mature { get; set; } = true;

	[JsonPropertyName( "drawn" )]
	public bool Drawn { get; set; } = false;
}
