﻿using Fumen.ChartDefinition;
using Microsoft.Xna.Framework.Graphics;
using static Fumen.Utils;

namespace StepManiaEditor
{
	public class EditorStopEvent : EditorRateAlteringEvent
	{
		public Stop StopEvent;

		private const string Format = "%.9gs";
		private bool WidthDirty;

		public double DoubleValue
		{
			get => ToSeconds(StopEvent.LengthMicros);
			set
			{
				var newMicros = ToMicros(value);
				if (StopEvent.LengthMicros != newMicros)
				{
					StopEvent.LengthMicros = newMicros;
					WidthDirty = true;
					EditorChart.OnRateAlteringEventModified(this);
				}
			}
		}

		/// <remarks>
		/// This lazily updates the width if it is dirty.
		/// This is a bit of hack because in order to determine the width we need to call into
		/// ImGui but that is not a thread-safe operation. If we were to set the width when
		/// loading the chart for example, this could crash. By lazily setting it we avoid this
		/// problem as long as we assume the caller of GetW() happens on the main thread.
		/// </remarks>
		public override double GetW()
		{
			if (WidthDirty)
			{
				SetW(ImGuiLayoutUtils.GetMiscEditorEventDragDoubleWidgetWidth(DoubleValue, Format));
				WidthDirty = false;
			}
			return base.GetW();
		}

		public EditorStopEvent(EditorChart editorChart, Stop chartEvent) : base(editorChart, chartEvent)
		{
			StopEvent = chartEvent;
			WidthDirty = true;
		}

		public override void Draw(TextureAtlas textureAtlas, SpriteBatch spriteBatch)
		{
			ImGuiLayoutUtils.MiscEditorEventDragDoubleWidget(
				GetImGuiId(),
				this,
				nameof(DoubleValue),
				(int)GetX(), (int)GetY(), (int)GetW(),
				Utils.UIStopColorABGR,
				false,
				CanBeDeleted,
				Format);
		}
	}
}
