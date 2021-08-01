using System;
using System.Threading;

namespace CCSTournament
{
	class MainClass
	{
		static int round;

		public static void Main(string[] args)
		{
			if (args.Length > 0)
				Room.banlistPath = args[0];
			if (args.Length > 1)
				Room.banlistName = args[1];
			Tournament t = new Tournament(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O" });
			Console.WriteLine(t);
			t.ProcessRound();
			Console.WriteLine(t);
		}
	}
}
