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
			Tournament t = new Tournament(new string[] { "A", "B", "C", "D" }, 2, "85.214.233.223");
			Console.WriteLine(t);
			for(int i = 0; i < 7; i++)
			{
				t.ProcessRound();
				Console.WriteLine(t);
			}
		}
	}
}
