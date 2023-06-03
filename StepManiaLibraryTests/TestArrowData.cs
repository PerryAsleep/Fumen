using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StepManiaLibrary;
using static Fumen.Converters.SMCommon;
using System.Collections.Generic;

namespace StepManiaLibraryTests
{
	/// <summary>
	/// Tests for ArrowData.
	/// </summary>
	[TestClass]
	public class TestArrowData
	{
		private static readonly ChartType[] PadDataTypes = new ChartType[]
		{
			ChartType.dance_single,
			ChartType.dance_double,
			ChartType.dance_solo,
			ChartType.dance_threepanel,
			ChartType.pump_single,
			ChartType.pump_halfdouble,
			ChartType.pump_double,
			ChartType.smx_beginner,
			ChartType.smx_single,
			ChartType.smx_dual,
			ChartType.smx_full,
		};

		private static readonly Dictionary<ChartType, PadData> TestPadData;

		static TestArrowData()
		{
			TestPadData = new Dictionary<ChartType, PadData>();
			var tasks = new Task<PadData>[PadDataTypes.Length];
			for (var i = 0; i < PadDataTypes.Length; i++)
			{
				var typeStr = ChartTypeString(PadDataTypes[i]);
				tasks[i] = PadData.LoadPadData(typeStr, $"{typeStr}.json");
			}

			// ReSharper disable once CoVariantArrayConversion
			Task.WaitAll(tasks);
			for (var i = 0; i < PadDataTypes.Length; i++)
			{
				TestPadData[PadDataTypes[i]] = tasks[i].Result;
			}
		}

		/// <summary>
		/// Tests that expected pad data can fit within other pad data.
		/// </summary>
		[TestMethod]
		public void TestCanFitWithin()
		{
			foreach (var kvp in TestPadData)
			{
				Assert.IsTrue(kvp.Value.CanFitWithin(kvp.Value));
			}

			var expectedFits = new Dictionary<ChartType, HashSet<ChartType>>
			{
				[ChartType.dance_single] = new HashSet<ChartType>
				{
					ChartType.dance_double,
					ChartType.dance_solo,
					ChartType.smx_single,
					ChartType.smx_full,
				},
				[ChartType.dance_double] = new HashSet<ChartType>
				{
					ChartType.smx_full,
				},
				[ChartType.dance_threepanel] = new HashSet<ChartType>
				{
					ChartType.dance_solo,
				},
				[ChartType.pump_single] = new HashSet<ChartType>
				{
					ChartType.pump_double,
				},
				[ChartType.pump_halfdouble] = new HashSet<ChartType>
				{
					ChartType.pump_double,
				},
				[ChartType.smx_beginner] = new HashSet<ChartType>
				{
					ChartType.dance_solo,
					ChartType.smx_single,
					ChartType.smx_dual,
					ChartType.smx_full,
				},
				[ChartType.smx_single] = new HashSet<ChartType>
				{
					ChartType.smx_full,
				},
				[ChartType.smx_dual] = new HashSet<ChartType>
				{
					ChartType.smx_full,
				},
			};

			for (var t1 = 0; t1 < PadDataTypes.Length; t1++)
			{
				for (var t2 = 0; t2 < PadDataTypes.Length; t2++)
				{
					var p1 = TestPadData[PadDataTypes[t1]];
					var p2 = TestPadData[PadDataTypes[t2]];

					if (t1 == t2)
					{
						Assert.IsTrue(p1.CanFitWithin(p2));
					}
					else
					{
						if (expectedFits.ContainsKey(PadDataTypes[t1]))
						{
							Assert.AreEqual(expectedFits[PadDataTypes[t1]].Contains(PadDataTypes[t2]), p1.CanFitWithin(p2));
						}
						else
						{
							Assert.IsFalse(p1.CanFitWithin(p2));
						}
					}
				}
			}
		}
	}
}
