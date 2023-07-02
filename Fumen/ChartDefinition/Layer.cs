using System.Collections.Generic;

namespace Fumen.ChartDefinition;

/// <summary>
/// Layer data.
/// A Layer has ordered Events.
/// </summary>
public class Layer
{
	public List<Event> Events { get; set; } = new();
}
