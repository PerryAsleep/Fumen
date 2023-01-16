using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ChartGeneratorTests.Utils;
using static Fumen.Utils;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Tests for SSC files.
	/// </summary>
	[TestClass]
	public class TestSSC
	{
		/// <summary>
		/// Helper method for checking the first few boilerplate events in test files.
		/// </summary>
		/// <param name="e">Events from the test Chart.</param>
		/// <param name="i">Event index. Will be incremented.</param>
		/// <param name="expectedTempo">Expected BPM for Tempo Event.</param>
		/// <param name="smFile">Whether or not this Chart is from an SM File.</param>
		private void CheckExpectedChartStartingTimingEvents(
			List<Event> e,
			ref int i,
			double expectedTempo = 120.0,
			bool smFile = false)
		{
			if (e[i] is TimeSignature ts)
			{
				Assert.AreEqual(4, ts.Signature.Numerator);
				Assert.AreEqual(4, ts.Signature.Denominator);
				AssertPositionMatches(ts, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is Tempo t)
			{
				Assert.AreEqual(expectedTempo, t.TempoBPM);
				AssertPositionMatches(t, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			// SM Files do not have the remaining events.
			if (smFile)
				return;

			if (e[i] is ScrollRate sr)
			{
				Assert.AreEqual(1.0, sr.Rate);
				AssertPositionMatches(sr, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is ScrollRateInterpolation sri)
			{
				Assert.AreEqual(1.0, sri.Rate);
				Assert.AreEqual(0, sri.PeriodLengthIntegerPosition);
				Assert.AreEqual(0.0, sri.PeriodTimeSeconds);
				AssertPositionMatches(sri, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is TickCount tc)
			{
				Assert.AreEqual(4, tc.Ticks);
				AssertPositionMatches(tc, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is Multipliers m)
			{
				Assert.AreEqual(1.0, m.HitMultiplier);
				Assert.AreEqual(1.0, m.MissMultiplier);
				AssertPositionMatches(m, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is Label l)
			{
				AssertPositionMatches(l, 0, 0.0, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

		}

		/// <summary>
		/// Checks event MetricPositions, integer positions, and time in microseconds
		/// on a Chart using common time (4/4).
		/// </summary>
		[TestMethod]
		public void TestTimeSignature44()
		{
			var s = LoadSSCSong(GetTestChartPath("TestTimeSignature44", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 39;
			Assert.AreEqual(numEvents, e.Count);
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			for (var eventIndex = numNonTapEvents; eventIndex < numEvents; eventIndex++)
			{
				var noteIndex = eventIndex - numNonTapEvents;
				var integerPos = noteIndex * 12;
				var time = noteIndex * secondsBetweenSixteenths;
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;
				AssertPositionMatches(e[eventIndex], integerPos, time, measure, beat, subDivisionNumerator, subDivisionDenominator);
			}
		}

		/// <summary>
		/// Checks event MetricPositions, integer positions, and time in microseconds
		/// when the Chart changes time signatures to odd values.
		/// </summary>
		[TestMethod]
		public void TestOddTimeSignaturesValid()
		{
			var s = LoadSSCSong(GetTestChartPath("TestOddTimeSignaturesValid", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			Assert.AreEqual(139, e.Count);
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					(li - ln) * secondsBetweenSixteenths,
					lm, lb, lfn, lfd);
				li++;
			}

			// First Measure: 4/4
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4);
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4);
			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4);

			// Second Measure: 7/8
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 7);
					Assert.AreEqual(ts.Signature.Denominator, 8);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 0);
				}
				else
				{
					Assert.Fail();
				}
				numNonTapEvents++;
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 4, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 4, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 5, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 5, 2, 4);
			CheckPos(ref i, numNonTapEvents, 1, 6, 0, 4);
			CheckPos(ref i, numNonTapEvents, 1, 6, 2, 4);

			// Third Measure: 3/4
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 3);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					CheckPos(ref i, numNonTapEvents, 2, 0, 0, 0);
				}
				else
				{
					Assert.Fail();
				}
				numNonTapEvents++;
			}
			CheckPos(ref i, numNonTapEvents, 2, 0, 0, 4);
			CheckPos(ref i, numNonTapEvents, 2, 0, 1, 4);
			CheckPos(ref i, numNonTapEvents, 2, 0, 2, 4);
			CheckPos(ref i, numNonTapEvents, 2, 0, 3, 4);
			CheckPos(ref i, numNonTapEvents, 2, 1, 0, 4);
			CheckPos(ref i, numNonTapEvents, 2, 1, 1, 4);
			CheckPos(ref i, numNonTapEvents, 2, 1, 2, 4);
			CheckPos(ref i, numNonTapEvents, 2, 1, 3, 4);
			CheckPos(ref i, numNonTapEvents, 2, 2, 0, 4);
			CheckPos(ref i, numNonTapEvents, 2, 2, 1, 4);
			CheckPos(ref i, numNonTapEvents, 2, 2, 2, 4);
			CheckPos(ref i, numNonTapEvents, 2, 2, 3, 4);

			// Fourth Measure: 3/4
			CheckPos(ref i, numNonTapEvents, 3, 0, 0, 4);
			CheckPos(ref i, numNonTapEvents, 3, 0, 1, 4);
			CheckPos(ref i, numNonTapEvents, 3, 0, 2, 4);
			CheckPos(ref i, numNonTapEvents, 3, 0, 3, 4);
			CheckPos(ref i, numNonTapEvents, 3, 1, 0, 4);
			CheckPos(ref i, numNonTapEvents, 3, 1, 1, 4);
			CheckPos(ref i, numNonTapEvents, 3, 1, 2, 4);
			CheckPos(ref i, numNonTapEvents, 3, 1, 3, 4);
			CheckPos(ref i, numNonTapEvents, 3, 2, 0, 4);
			CheckPos(ref i, numNonTapEvents, 3, 2, 1, 4);
			CheckPos(ref i, numNonTapEvents, 3, 2, 2, 4);
			CheckPos(ref i, numNonTapEvents, 3, 2, 3, 4);

			// Fifth Measure: 2/1
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 2);
					Assert.AreEqual(ts.Signature.Denominator, 1);
					CheckPos(ref i, numNonTapEvents, 4, 0, 0, 0);
				}
				else
				{
					Assert.Fail();
				}
				numNonTapEvents++;
			}
			CheckPos(ref i, numNonTapEvents, 4, 0, 0, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 1, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 2, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 3, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 4, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 5, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 6, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 7, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 8, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 9, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 10, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 11, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 12, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 13, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 14, 16);
			CheckPos(ref i, numNonTapEvents, 4, 0, 15, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 0, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 1, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 2, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 3, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 4, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 5, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 6, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 7, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 8, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 9, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 10, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 11, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 12, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 13, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 14, 16);
			CheckPos(ref i, numNonTapEvents, 4, 1, 15, 16);

			// Sixth Measure: 2/1
			CheckPos(ref i, numNonTapEvents, 5, 0, 0, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 1, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 2, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 3, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 4, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 5, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 6, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 7, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 8, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 9, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 10, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 11, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 12, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 13, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 14, 16);
			CheckPos(ref i, numNonTapEvents, 5, 0, 15, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 0, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 1, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 2, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 3, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 4, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 5, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 6, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 7, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 8, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 9, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 10, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 11, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 12, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 13, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 14, 16);
			CheckPos(ref i, numNonTapEvents, 5, 1, 15, 16);

			// Seventh Measure: 4/4
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 4);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					CheckPos(ref i, numNonTapEvents, 6, 0, 0, 0);
				}
				else
				{
					Assert.Fail();
				}
				numNonTapEvents++;
			}
			CheckPos(ref i, numNonTapEvents, 6, 0, 0, 4);
			CheckPos(ref i, numNonTapEvents, 6, 0, 1, 4);
			CheckPos(ref i, numNonTapEvents, 6, 0, 2, 4);
			CheckPos(ref i, numNonTapEvents, 6, 0, 3, 4);
			CheckPos(ref i, numNonTapEvents, 6, 1, 0, 4);
			CheckPos(ref i, numNonTapEvents, 6, 1, 1, 4);
			CheckPos(ref i, numNonTapEvents, 6, 1, 2, 4);
			CheckPos(ref i, numNonTapEvents, 6, 1, 3, 4);
			CheckPos(ref i, numNonTapEvents, 6, 2, 0, 4);
			CheckPos(ref i, numNonTapEvents, 6, 2, 1, 4);
		}

		/// <summary>
		/// Checks that if a chart changes time signatures at a time not on a measure
		/// boundary that we fall back to 4/4.
		/// </summary>
		[TestMethod]
		public void TestOddTimeSignaturesBadMeasureBoundary()
		{
			var s = LoadSSCSong(GetTestChartPath("TestOddTimeSignaturesBadMeasureBoundary", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 132;
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					(li - ln) * secondsBetweenSixteenths,
					lm, lb, lfn, lfd);
				li++;
			}

			// All Measure are 4/4 due to fallback.
			while (i < numEvents)
			{
				var noteIndex = i - numNonTapEvents;
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;
				CheckPos(ref i, numNonTapEvents, measure, beat, subDivisionNumerator, subDivisionDenominator);
			}
		}

		/// <summary>
		/// Checks that if a chart uses an unsupported time signature that we fall back to 4/4.
		/// </summary>
		[TestMethod]
		public void TestOddTimeSignaturesUnsupportedSignature()
		{
			var s = LoadSSCSong(GetTestChartPath("TestOddTimeSignaturesUnsupportedSignature", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 132;
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					(li - ln) * secondsBetweenSixteenths,
					lm, lb, lfn, lfd);
				li++;
			}

			// All Measure are 4/4 due to fallback.
			while (i < numEvents)
			{
				var noteIndex = i - numNonTapEvents;
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;
				CheckPos(ref i, numNonTapEvents, measure, beat, subDivisionNumerator, subDivisionDenominator);
			}
		}

		/// <summary>
		/// Checks that stop timings affect event TimeSeconds but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestStopTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestStopTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var i = 0;
			Assert.AreEqual(42, e.Count);
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, double tst)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					(li - ln) * secondsBetweenSixteenths + tst,
					lm, lb, lfn, lfd);
				li++;
			}

			var stopTime = 0.0;
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
			// Stop at 0.000 for 1.111
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 1.111);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, stopTime);
			// Stop at 0.499 (rounded to half a beat) for 0.666
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 0.666);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, stopTime);
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, stopTime);
			// Stop at 4.000 for 99.999
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 99.999);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, stopTime);
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, stopTime);
		}

		/// <summary>
		/// Checks that Delays affect event TimeSeconds but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestDelayTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestDelayTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var i = 0;
			Assert.AreEqual(42, e.Count);
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, double tst)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					(li - ln) * secondsBetweenSixteenths + tst,
					lm, lb, lfn, lfd);
				li++;
			}

			var stopTime = 0.0;
			// Delay at 0.000 for 1.111
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 1.111);
					Assert.IsTrue(stop.IsDelay);
					CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
					numNonTapEvents++; 
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, stopTime);
			// Stop at 0.499 (rounded to half a beat) for 0.666
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 0.666);
					Assert.IsTrue(stop.IsDelay);
					CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, stopTime);
					numNonTapEvents++;
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, stopTime);
			// Stop at 4.000 for 99.999
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthSeconds, 99.999);
					Assert.IsTrue(stop.IsDelay);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, stopTime);
					numNonTapEvents++;
					stopTime += stop.LengthSeconds;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, stopTime);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, stopTime);
		}

		/// <summary>
		/// Checks that tempo changes affect event TimeSeconds but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestTempoChangeTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestTempoChangeTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var i = 0;
			Assert.AreEqual(42, e.Count);
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, double tm)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					tm,
					lm, lb, lfn, lfd);
				li++;
			}
			
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, 0.0);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, 0.125000);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, 0.250000);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, 0.375000);
			// BPM at 1.000000: 240.000000
			{
				if (e[i] is Tempo tc)
				{
					Assert.AreEqual(tc.TempoBPM, 240.0);
					CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.500000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.500000);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, 0.562500);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, 0.625000);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, 0.687500);
			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, 0.750000);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, 0.812500);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, 0.875000);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, 0.937500);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, 1.000000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, 1.062500);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, 1.125000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, 1.187500);
			// BPM at 3.999999 (rounded to 4.0): 60.000000
			{
				if (e[i] is Tempo tc)
				{
					Assert.AreEqual(tc.TempoBPM, 60.0);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.250000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.250000);

			// BPM at 4.251000 (rounded to 4.25): 120.000000
			{
				if (e[i] is Tempo tc)
				{
					Assert.AreEqual(tc.TempoBPM, 120.0);
					CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1.500000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, 1.625000);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, 1.750000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1.875000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, 2.000000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, 2.125000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, 2.250000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, 2.375000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, 2.500000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, 2.625000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, 2.750000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, 2.875000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, 3.000000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, 3.125000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, 3.250000);
		}

		/// <summary>
		/// Checks that stutter gimmicks function as intended without affecting TimeSeconds, IntegerPosition, or MetricPosition
		/// </summary>
		[TestMethod]
		public void TestStutterGimmickTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestStutterGimmickTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 72;
			Assert.AreEqual(numEvents, e.Count);
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i, 240.0);

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;

			var numNonTapEvents = i;
			var eventIndex = numNonTapEvents;
			while(eventIndex < numEvents)
			{
				var noteIndex = eventIndex - numNonTapEvents;
				var integerPos = noteIndex * 12;
				var time = noteIndex * secondsBetweenSixteenths;
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;

				// Tap note
				AssertPositionMatches(e[eventIndex], integerPos, time, measure, beat, subDivisionNumerator, subDivisionDenominator);
				eventIndex++;

				// Stop
				if (eventIndex < numEvents)
				{
					if (e[eventIndex] is Stop stop)
					{
						Assert.AreEqual(stop.LengthSeconds, 0.0625);
						AssertPositionMatches(e[eventIndex], integerPos, time, measure, beat, subDivisionNumerator,
							subDivisionDenominator);
						numNonTapEvents++;
					}
					else
					{
						Assert.Fail();
					}
					eventIndex++;
				}
			}
		}

		/// <summary>
		/// Checks that Warps affect event TimeSeconds but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestWarpTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestWarpTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 42;
			Assert.AreEqual(numEvents, e.Count);
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i);

			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, double tm)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					tm,
					lm, lb, lfn, lfd);
				li++;
			}

			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, 0.0);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, 0.125000);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, 0.250000);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, 0.375000);

			// Warp at 1.000000 for 1 beat (48 rows)
			{
				if (e[i] is Warp warp)
				{
					Assert.AreEqual(48, warp.LengthIntegerPosition);
					CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.500000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.500000);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, 0.500000);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, 0.500000);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, 0.500000);

			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, 0.500000);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, 0.625000);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, 0.750000);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, 0.875000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, 1.000000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, 1.125000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, 1.250000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, 1.375000);

			// Warp at 4.000000 for 3 beats (144 rows)
			{
				if (e[i] is Warp warp)
				{
					Assert.AreEqual(144, warp.LengthIntegerPosition);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.500000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, 1.500000);
			// Warp at 5.000000 for 1 beat (48 rows)
			// This Warp is inside the previous Warp. They should not stack.
			{
				if (e[i] is Warp warp)
				{
					Assert.AreEqual(48, warp.LengthIntegerPosition);
					CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1.500000);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, 1.500000);

			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, 1.500000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, 1.625000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, 1.750000);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, 1.875000);
		}

		/// <summary>
		/// Checks that negative Stops affect event TimeSeconds but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestNegativeStopTiming()
		{
			// Load the sm file here.
			// This sm file is the same as the ssc file from TestWarpTiming. StepMania generated
			// both files from the same Chart.
			var s = LoadSMSong(GetTestChartPath("TestNegativeStopTiming", "test", "sm"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			Assert.AreEqual(37, e.Count);
			var i = 0;
			CheckExpectedChartStartingTimingEvents(e, ref i, 120.0, true);

			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, double ts)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					ts,
					lm, lb, lfn, lfd);
				li++;
			}

			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, 0.0);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, 0.125);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, 0.250);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, 0.375);

			// Negative Stop at 1.000000 for 1 beat (-0.5s)
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(-0.5, stop.LengthSeconds);
					CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.5);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 0.5);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, 0.5);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, 0.5);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, 0.5);

			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, 0.5);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, 0.625);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, 0.750);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, 0.875);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, 1.000);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, 1.125);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, 1.250);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, 1.375);

			// Negative Stop at 4.000000 for 3 beats (-1.5s)
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(-1.5, stop.LengthSeconds);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.5);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, 1.5);
			// Negative Stop at 5.000000 for 1 beat (-1.5s)
			// This negative Stop is inside the previous negative Stop. Unlike Warps, they should stack.
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(-0.5, stop.LengthSeconds);
					CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1.5);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, 1.5);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, 1.5);
		}

		[TestMethod]
		public void TestSaveLoadNoDiff()
		{
			// TODO: Implement unit tests to check that no unexpected diffs occur when loading and saving songs.
		}

		/// <summary>
		/// Test measure spacing that StepMania doesn't support, like 11 notes per measure.
		/// When we load this we should record the original spacing, but snap it to supported spacing like StepMania.
		/// When saving using UseSourceExtraOriginalMeasurePosition, we should save back out the unaltered spacing.
		/// When saving using UseLeastCommonMultipleFromStepmaniaEditor, we should save the altered spacing.
		/// </summary>
		[TestMethod]
		public void TestSaveLoadUnsupportedSpacing()
		{
			void CheckSong(Song s, bool spacingShouldBeMinimized)
			{
				Assert.AreEqual(s.Charts.Count, 1);
				var e = s.Charts[0].Layers[0].Events;
				var numEvents = 85;
				Assert.AreEqual(numEvents, e.Count);
				var i = 0;
				CheckExpectedChartStartingTimingEvents(e, ref i);

				// Each measure in this chart has that many numbers of notes evenly dividing it.
				// First measure has 1 note, second has 2, third has 3, etc.
				// These original fractions should be stored in the Extras under TagFumenNoteOriginalMeasurePosition.
				// The actual MetricPositions however should be snapped to 48th notes.
				var bpm = 120.0;
				for (var measure = 0; measure < 12; measure++)
				{
					var notesInMeasure = measure + 1;
					for (var note = 0; note < notesInMeasure; note++)
					{
						var rowInMeasure = Convert.ToInt32(((double)note / notesInMeasure) * 192.0);
						var integerPosition = rowInMeasure + measure * 192;
						var beat = (integerPosition - (measure * 192)) / 48;
						var numerator = integerPosition - (measure * 192) - (beat * 48);
						var time = integerPosition * (60.0 / bpm / 48);

						if (e[i].Extras.TryGetSourceExtra(SMCommon.TagFumenNoteOriginalMeasurePosition, out Fraction f))
						{
							if (spacingShouldBeMinimized)
							{
								Assert.AreEqual(f.Numerator, note);
								Assert.AreEqual(f.Denominator, notesInMeasure);
							}
							else
							{
								// The SourceExtra fraction is position in the whole measure, not the beat subdivision.
								var expectedFraction = new Fraction(beat * 48 + numerator, 192).Reduce();
								Assert.AreEqual(f.Reduce(), expectedFraction);
							}
						}
						else
						{
							Assert.Fail();
						}

						AssertPositionMatches(e[i], integerPosition, time, measure, beat, numerator, 48);
						i++;
					}
				}
			}

			// Load and check the test song.
			var song = LoadSSCSong(GetTestChartPath("TestSaveLoadUnsupportedSpacing", "test", "ssc"));
			CheckSong(song, true);

			// Save this file with UseSourceExtraOriginalMeasurePosition reload it to ensure we preserve the spacing.
			var newFile = System.IO.Path.GetTempFileName();
			var config = new SMWriterBase.SMWriterBaseConfig
			{
				FilePath = newFile,
				Song = song,
				MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseSourceExtraOriginalMeasurePosition,
				PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource,
				WriteTemposFromExtras = true,
				WriteStopsFromExtras = true,
				WriteDelaysFromExtras = true,
				WriteWarpsFromExtras = true,
				WriteScrollsFromExtras = true,
				WriteSpeedsFromExtras = true,
				WriteTimeSignaturesFromExtras = true,
				WriteTickCountsFromExtras = true,
				WriteLabelsFromExtras = true,
				WriteFakesFromExtras = true,
				WriteCombosFromExtras = true,
			};
			new SSCWriter(config).Save();
			Task.Run(async () => { song = await new SSCReader(newFile).LoadAsync(CancellationToken.None); }).Wait();
			CheckSong(song, true);

			// Save this file with UseLeastCommonMultipleFromStepmaniaEditor reload it to ensure use altered spacing.
			newFile = System.IO.Path.GetTempFileName();
			config = new SMWriterBase.SMWriterBaseConfig
			{
				FilePath = newFile,
				Song = song,
				MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseLeastCommonMultipleFromStepmaniaEditor,
				PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource,
				WriteTemposFromExtras = true,
				WriteStopsFromExtras = true,
				WriteDelaysFromExtras = true,
				WriteWarpsFromExtras = true,
				WriteScrollsFromExtras = true,
				WriteSpeedsFromExtras = true,
				WriteTimeSignaturesFromExtras = true,
				WriteTickCountsFromExtras = true,
				WriteLabelsFromExtras = true,
				WriteFakesFromExtras = true,
				WriteCombosFromExtras = true,
			};
			new SSCWriter(config).Save();
			Task.Run(async () => { song = await new SSCReader(newFile).LoadAsync(CancellationToken.None); }).Wait();
			CheckSong(song, false);
		}
	}
}
