using System;
using System.IO;
using System.Threading;

namespace CCSTournament
{
	class MainClass
	{
		private struct JsonThing
		{
			public string[] participants;
		}

		public static void Main(string[] args)
		{
			string[] participants = null;
			foreach(string arg in args)
			{
				if(arg == "help")
				{
					Console.WriteLine("Parameter syntax: parameter name=parameter value\nAvailable parameters: banlistPath, banlistName, participants (either json or txt file)\nparticipants format: json: single array named participants containing the names; txt: One name per line");
					return;
				}
				string[] kv = arg.Split('=');
				if (kv.Length != 2) continue;
				switch (kv[0])
				{
					case "banlistPath":
						Room.banlistPath = kv[1];
						break;
					case "banlistName":
						Room.banlistName = kv[1];
						break;
					case "participants":
						if (kv[1].EndsWith(".json", StringComparison.Ordinal)) 
						{
							JsonThing obj = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonThing>(File.ReadAllText(kv[1]));
							participants = obj.participants;
						}
						else if(kv[1].EndsWith(".txt", StringComparison.Ordinal))
						{
							participants = File.ReadAllLines(kv[1]);
						}
						break;
				}
			}
			if(participants == null)
			{
				Console.WriteLine("no participants list supplied");
				return;
			}
			Tournament t = new Tournament(participants, 4, "85.214.233.223");
			do
			{
				Console.WriteLine(t);
			}
			while (t.ProcessRound());
		}
	}
}
