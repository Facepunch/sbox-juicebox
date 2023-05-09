using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Juicebox;

public class JuiceboxDisplay
{
	public JuiceboxHeader Header { get; set; }
	public JuiceboxStage Stage { get; set; }
	public JuiceboxForm Form { get; set; }
}

public class JuiceboxHeader
{
	public int? RoundNumber { get; set; }
	public int? RoundTime { get; set; }
}

public class JuiceboxStage
{
	public string Title { get; set; }
	public string Subtitle { get; set; }
}

public class JuiceboxForm
{
	public string Header { get; set; }

	public string SubmitLabel { get; set; } = "Submit";

	[JsonIgnore]
	public List<JuiceboxFormControl> Controls { get; set; }

	[JsonPropertyName( nameof( Controls ) ), EditorBrowsable( EditorBrowsableState.Never )]
	public IReadOnlyList<object> ControlsTypeless => Controls; // hack to force serializing polymorphic fields
}

public abstract class JuiceboxFormControl
{
	public abstract string Type { get; }
	public string Key { get; set; }
}

public class JuiceboxInput : JuiceboxFormControl
{
	public override string Type => "Input";
	public string Label { get; set; }
	public string Placeholder { get; set; }
	public int MaxLength { get; set; }
	public string Value { get; set; }
}

public class JuiceboxTextarea : JuiceboxFormControl
{
	public override string Type => "Textarea";
	public string Label { get; set; }
	public string Placeholder { get; set; }
	public int MaxLength { get; set; }
	public int Rows { get; set; }
	public string Value { get; set; }
}

public class JuiceboxRadioGroup : JuiceboxFormControl
{
	public override string Type => "RadioGroup";
	public List<JuiceboxRadioOption> Options { get; set; }
}

public class JuiceboxRadioOption
{
	public string Label { get; set; }
	public string Value { get; set; }
}

public class JuiceboxDrawing : JuiceboxFormControl
{
	public override string Type => "Drawing";
	public int Width { get; set; }
	public int Height { get; set; }
}

public class JuiceboxButton : JuiceboxFormControl
{
	public override string Type => "Button";
	public string Label { get; set; }
	public string Variant { get; set; }
}
