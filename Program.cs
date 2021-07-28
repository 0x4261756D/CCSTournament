using System;

namespace serverCreator
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			if (args.Length > 0)
				Room.banlistPath = args[0];
			if (args.Length > 1)
				Room.banlistName = args[1];
			Console.WriteLine(Room.banlistName + Room.banlistPath);
		}
	}
}
