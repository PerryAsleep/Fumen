using System;
using System.Threading;
using System.Threading.Tasks;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ChartGeneratorTests.Utils;

namespace ChartGeneratorTests
{
	/// <summary>
	/// Tests for SSC files.
	/// </summary>
	[TestClass]
	public class TestSSC
	{
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
			var numEvents = 34;
			Assert.AreEqual(numEvents, e.Count);
			if (e[0] is TimeSignature ts)
			{
				Assert.AreEqual(ts.Signature.Numerator, 4);
				Assert.AreEqual(ts.Signature.Denominator, 4);
				AssertPositionMatches(ts, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}

			if (e[1] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 120.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;

			var numNonTapEvents = 2;
			for (var eventIndex = numNonTapEvents; eventIndex < numEvents; eventIndex++)
			{
				var noteIndex = eventIndex - numNonTapEvents;
				var integerPos = noteIndex * 12;
				var timeMicros = Convert.ToInt64(noteIndex * secondsBetweenSixteenths * 1000000.0);
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;
				AssertPositionMatches(e[eventIndex], integerPos, timeMicros, measure, beat, subDivisionNumerator, subDivisionDenominator);
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
			var numEvents = 134;
			var i = 0;
			Assert.AreEqual(numEvents, e.Count);
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 4);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					AssertPositionMatches(ts, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}
			}
			i++;

			if (e[i] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 120.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					Convert.ToInt64((li - ln) * secondsBetweenSixteenths * 1000000.0),
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
			var numEvents = 130;
			var i = 0;
			Assert.AreEqual(numEvents, e.Count);
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 4);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					AssertPositionMatches(ts, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}
			}
			i++;

			if (e[i] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 120.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					Convert.ToInt64((li - ln) * secondsBetweenSixteenths * 1000000.0),
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
			var numEvents = 130;
			var i = 0;
			Assert.AreEqual(numEvents, e.Count);
			{
				if (e[i] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 4);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					AssertPositionMatches(ts, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}
			}
			i++;

			if (e[i] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 120.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					Convert.ToInt64((li - ln) * secondsBetweenSixteenths * 1000000.0),
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
		/// Checks that stop timings affect event TimeMicros but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestStopTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestStopTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 37;
			var i = 0;
			Assert.AreEqual(numEvents, e.Count);
			if (e[i] is TimeSignature ts)
			{
				Assert.AreEqual(ts.Signature.Numerator, 4);
				Assert.AreEqual(ts.Signature.Denominator, 4);
				AssertPositionMatches(ts, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			if (e[i] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 120.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;
			
			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, long tst)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					Convert.ToInt64((li - ln) * secondsBetweenSixteenths * 1000000.0) + tst,
					lm, lb, lfn, lfd);
				li++;
			}

			var stopTime = 0L;
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
			// Stop at 0.000 for 1.111
			{
				if (e[i] is Stop stop)
				{
					Assert.AreEqual(stop.LengthMicros, 1111000);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, stopTime);
					stopTime += stop.LengthMicros;
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
					Assert.AreEqual(stop.LengthMicros, 666000);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, stopTime);
					stopTime += stop.LengthMicros;
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
					Assert.AreEqual(stop.LengthMicros, 99999000);
					numNonTapEvents++;
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, stopTime);
					stopTime += stop.LengthMicros;
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
		/// Checks that tempo changes affect event TimeMicros but not IntegerPosition or MetricPosition.
		/// </summary>
		[TestMethod]
		public void TestTempoChangeTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestTempoChangeTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 37;
			var i = 0;
			Assert.AreEqual(numEvents, e.Count);
			if (e[i] is TimeSignature ts)
			{
				Assert.AreEqual(ts.Signature.Numerator, 4);
				Assert.AreEqual(ts.Signature.Denominator, 4);
				AssertPositionMatches(ts, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}
			i++;

			{
				if (e[i] is TempoChange tc)
				{
					Assert.AreEqual(tc.TempoBPM, 120.0);
					AssertPositionMatches(tc, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}
			}
			i++;
			
			var numNonTapEvents = i;
			void CheckPos(ref int li, int ln, int lm, int lb, int lfn, int lfd, long tm)
			{
				AssertPositionMatches(
					e[li],
					(li - ln) * 12,
					tm,
					lm, lb, lfn, lfd);
				li++;
			}
			
			CheckPos(ref i, numNonTapEvents, 0, 0, 0, 4, 0L);
			CheckPos(ref i, numNonTapEvents, 0, 0, 1, 4, 125000L);
			CheckPos(ref i, numNonTapEvents, 0, 0, 2, 4, 250000L);
			CheckPos(ref i, numNonTapEvents, 0, 0, 3, 4, 375000L);
			// BPM at 1.000000: 240.000000
			{
				if (e[i] is TempoChange tc)
				{
					Assert.AreEqual(tc.TempoBPM, 240.0);
					CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 500000L);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 0, 1, 0, 4, 500000L);
			CheckPos(ref i, numNonTapEvents, 0, 1, 1, 4, 562500L);
			CheckPos(ref i, numNonTapEvents, 0, 1, 2, 4, 625000L);
			CheckPos(ref i, numNonTapEvents, 0, 1, 3, 4, 687500L);
			CheckPos(ref i, numNonTapEvents, 0, 2, 0, 4, 750000L);
			CheckPos(ref i, numNonTapEvents, 0, 2, 1, 4, 812500L);
			CheckPos(ref i, numNonTapEvents, 0, 2, 2, 4, 875000L);
			CheckPos(ref i, numNonTapEvents, 0, 2, 3, 4, 937500L);
			CheckPos(ref i, numNonTapEvents, 0, 3, 0, 4, 1000000L);
			CheckPos(ref i, numNonTapEvents, 0, 3, 1, 4, 1062500L);
			CheckPos(ref i, numNonTapEvents, 0, 3, 2, 4, 1125000L);
			CheckPos(ref i, numNonTapEvents, 0, 3, 3, 4, 1187500L);
			// BPM at 3.999999 (rounded to 4.0): 60.000000
			{
				if (e[i] is TempoChange tc)
				{
					Assert.AreEqual(tc.TempoBPM, 60.0);
					CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1250000L);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 0, 4, 1250000L);

			// BPM at 4.251000 (rounded to 4.25): 120.000000
			{
				if (e[i] is TempoChange tc)
				{
					Assert.AreEqual(tc.TempoBPM, 120.0);
					CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1500000L);
					numNonTapEvents++;
				}
				else
				{
					Assert.Fail();
				}
			}
			CheckPos(ref i, numNonTapEvents, 1, 0, 1, 4, 1500000L);
			CheckPos(ref i, numNonTapEvents, 1, 0, 2, 4, 1625000L);
			CheckPos(ref i, numNonTapEvents, 1, 0, 3, 4, 1750000L);
			CheckPos(ref i, numNonTapEvents, 1, 1, 0, 4, 1875000L);
			CheckPos(ref i, numNonTapEvents, 1, 1, 1, 4, 2000000L);
			CheckPos(ref i, numNonTapEvents, 1, 1, 2, 4, 2125000L);
			CheckPos(ref i, numNonTapEvents, 1, 1, 3, 4, 2250000L);
			CheckPos(ref i, numNonTapEvents, 1, 2, 0, 4, 2375000L);
			CheckPos(ref i, numNonTapEvents, 1, 2, 1, 4, 2500000L);
			CheckPos(ref i, numNonTapEvents, 1, 2, 2, 4, 2625000L);
			CheckPos(ref i, numNonTapEvents, 1, 2, 3, 4, 2750000L);
			CheckPos(ref i, numNonTapEvents, 1, 3, 0, 4, 2875000L);
			CheckPos(ref i, numNonTapEvents, 1, 3, 1, 4, 3000000L);
			CheckPos(ref i, numNonTapEvents, 1, 3, 2, 4, 3125000L);
			CheckPos(ref i, numNonTapEvents, 1, 3, 3, 4, 3250000L);
		}

		/// <summary>
		/// Checks that stutter gimmicks function as intended without affecting TimeMicros, IntegerPosition, or MetricPosition
		/// </summary>
		[TestMethod]
		public void TestStutterGimmickTiming()
		{
			var s = LoadSSCSong(GetTestChartPath("TestStutterGimmickTiming", "test", "ssc"));
			Assert.AreEqual(s.Charts.Count, 1);
			var e = s.Charts[0].Layers[0].Events;
			var numEvents = 67;
			Assert.AreEqual(numEvents, e.Count);
			if (e[0] is TimeSignature ts)
			{
				Assert.AreEqual(ts.Signature.Numerator, 4);
				Assert.AreEqual(ts.Signature.Denominator, 4);
				AssertPositionMatches(ts, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}

			if (e[1] is TempoChange tc)
			{
				Assert.AreEqual(tc.TempoBPM, 240.0);
				AssertPositionMatches(tc, 0, 0L, 0);
			}
			else
			{
				Assert.Fail();
			}

			var bpm = 120.0;
			var secondsBetweenSixteenths = (60.0 / bpm) / 4;

			var numNonTapEvents = 2;
			var eventIndex = numNonTapEvents;
			while(eventIndex < numEvents)
			{
				var noteIndex = eventIndex - numNonTapEvents;
				var integerPos = noteIndex * 12;
				var timeMicros = Convert.ToInt64(noteIndex * secondsBetweenSixteenths * 1000000.0);
				var measure = noteIndex / 16;
				var beat = noteIndex / 4 - (measure * 4);
				var subDivisionNumerator = noteIndex % 4;
				var subDivisionDenominator = 4;

				// Tap note
				AssertPositionMatches(e[eventIndex], integerPos, timeMicros, measure, beat, subDivisionNumerator, subDivisionDenominator);
				eventIndex++;

				// Stop
				if (eventIndex < numEvents)
				{
					if (e[eventIndex] is Stop stop)
					{
						Assert.AreEqual(stop.LengthMicros, 62500);
						AssertPositionMatches(e[eventIndex], integerPos, timeMicros, measure, beat, subDivisionNumerator,
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

		[TestMethod]
		public void TestWarpTiming()
		{
			// TODO: Implement Warps properly and check behavior
		}

		[TestMethod]
		public void TestNegativeStopTiming()
		{
			// TODO: Implement Negative stops properly and check behavior
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
				var numEvents = 80;
				Assert.AreEqual(numEvents, e.Count);
				if (e[0] is TimeSignature ts)
				{
					Assert.AreEqual(ts.Signature.Numerator, 4);
					Assert.AreEqual(ts.Signature.Denominator, 4);
					AssertPositionMatches(ts, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}

				if (e[1] is TempoChange tc)
				{
					Assert.AreEqual(tc.TempoBPM, 120.0);
					AssertPositionMatches(tc, 0, 0L, 0);
				}
				else
				{
					Assert.Fail();
				}

				// Each measure in this chart has that many numbers of notes evenly dividing it.
				// First measure has 1 note, second has 2, third has 3, etc.
				// These original fractions should be stored in the Extras under TagFumenNoteOriginalMeasurePosition.
				// The actual MetricPositions however should be snapped to 48th notes.
				var bpm = 120.0;
				var i = 2;
				for (var measure = 0; measure < 12; measure++)
				{
					var notesInMeasure = measure + 1;
					for (var note = 0; note < notesInMeasure; note++)
					{
						var rowInMeasure = Convert.ToInt32(((double)note / notesInMeasure) * 192.0);
						var integerPosition = rowInMeasure + measure * 192;
						var beat = (integerPosition - (measure * 192)) / 48;
						var numerator = integerPosition - (measure * 192) - (beat * 48);
						var micros = Convert.ToInt64(1000000.0 * integerPosition * (60.0 / bpm) / 48);

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

						AssertPositionMatches(e[i], integerPosition, micros, measure, beat, numerator, 48);
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
				WriteBPMsFromExtras = true,
				WriteStopsFromExtras = true,
				WriteTimeSignaturesFromExtras = true
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
				WriteBPMsFromExtras = true,
				WriteStopsFromExtras = true,
				WriteTimeSignaturesFromExtras = true
			};
			new SSCWriter(config).Save();
			Task.Run(async () => { song = await new SSCReader(newFile).LoadAsync(CancellationToken.None); }).Wait();
			CheckSong(song, false);
		}
	}
}
