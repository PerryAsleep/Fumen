
namespace StepManiaLibrary.PerformedChart
{
	/// <summary>
	/// A PerformedChart contains a series of PerformanceNodes.
	/// Abstract base class for the various types of PerformanceNodes in a PerformedChart.
	/// </summary>
	public abstract class PerformanceNode
	{
		/// <summary>
		/// IntegerPosition of this node in the Chart.
		/// </summary>
		public int Position;

		/// <summary>
		/// Next PerformanceNode in the series.
		/// </summary>
		public PerformanceNode Next;

		/// <summary>
		/// Previous PerformanceNode in the series.
		/// </summary>
		public PerformanceNode Prev;
	}

	/// <summary>
	/// PerformanceNode representing a normal step or release.
	/// </summary>
	public class StepPerformanceNode : PerformanceNode, MineUtils.IChartNode
	{
		/// <summary>
		/// GraphNodeInstance representing the state at this PerformanceNode.
		/// </summary>
		public GraphNodeInstance GraphNodeInstance;

		/// <summary>
		/// GraphLinkInstance to the GraphNodeInstance at this PerformanceNode.
		/// </summary>
		public GraphLinkInstance GraphLinkInstance;

		#region MineUtils.IChartNode Implementation

		public GraphNode GetGraphNode()
		{
			return GraphNodeInstance?.Node;
		}

		public GraphLink GetGraphLinkToNode()
		{
			return GraphLinkInstance?.GraphLink;
		}

		public int GetPosition()
		{
			return Position;
		}

		#endregion
	}

	/// <summary>
	/// PerformanceNode representing a mine.
	/// </summary>
	public class MinePerformanceNode : PerformanceNode
	{
		/// <summary>
		/// The lane or arrow this Mine occurs on.
		/// </summary>
		public int Arrow;
	}
}
