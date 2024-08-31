using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM;
public static class GMRandom
{
	public static uint[] State = new uint[16];
	public static uint Seed;
	public static uint Index;
	public static uint RandomPoly = 0xDA442D24; // TODO : in older gm versions this was 0xDA442D20 incorrectly. there's a setting to go back to this i think?

	public static uint InitialiseRNG(uint seed)
	{
		var passedInSeed = seed;

		Seed = seed;
		var curState = seed;
		for (var i = 0; i < 16; i++)
		{
			State[i] = curState = ((curState * 214013) + 2531011) >> 16;
		}

		Index = 0;

		return passedInSeed;
	}

	// Generates a random uint, from 0 to uint.Max_Value (probably -1?)
	// Taken from https://github.com/YoYoGames/GameMaker-HTML5/blob/d1b6b3c407470e23d13c736f97cb64cb722c71d1/scripts/functions/Function_Maths.js#L460
	// (I've verified that this generates the same numbers as the C++ implementation)
	public static uint YYRandom()
	{

		var a = State[Index];
		var c = State[(Index - 3) & 15];
		var b = a ^ c ^ (a << 16) ^ (c << 15);
		c = State[(Index + 9) & 15];
		c ^= c >> 11;
		a = State[Index] = b ^ c;
		var d = a ^ ((a << 5) & RandomPoly);
		Index = (Index + 15) & 15;
		a = State[Index];
		State[Index] = a ^ b ^ d ^ (a << 2) ^ (b << 18) ^ (c << 28);
		return State[Index];
	}

	// Generates a random uint from 0 to n-1
	public static uint YYRandom(int n)
	{
		return YYRandom() % (uint)((n ^ n >> 31) - (n >> 31));
	}
}
