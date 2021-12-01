﻿using System;
using System.Threading.Tasks;
using FMOD;
using Fumen;

namespace StepManiaEditor
{
	public class SoundManager
	{
		private FMOD.System System;

		public SoundManager()
		{
			ErrCheck(Factory.System_Create(out System));
			ErrCheck(System.init(100, INITFLAGS.NORMAL, IntPtr.Zero));
		}

		public uint GetSampleRate()
		{
			ErrCheck(System.getSoftwareFormat(out var sampleRate, out _, out _));
			return (uint)sampleRate;
		}

		public async Task<Sound> LoadAsync(string fileName)
		{
			return await Task.Run(() =>
			{
				ErrCheck(System.createSound(fileName, MODE.DEFAULT, out var sound), $"Failed to load {fileName}");
				return sound;
			});
		}

		public static bool ErrCheck(RESULT result, string failureMessage = null)
		{
			if (result != RESULT.OK)
			{
				if (!string.IsNullOrEmpty(failureMessage))
				{
					Logger.Error($"{failureMessage} {result:G}");
				}
				else
				{
					Logger.Error($"FMOD error: {result:G}");
				}
				return false;
			}
			return true;
		}
	}
}