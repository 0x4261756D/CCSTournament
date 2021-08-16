using System;
using System.IO;
using Newtonsoft.Json;

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
			string serverIp = "localhost", dashboardUrl = "localhost", dashboardToken = "";
			foreach(string arg in args)
			{
				if(arg == "help")
				{
					Console.WriteLine("Parameter syntax: parameter name=parameter value\n" +
						"Available parameters: banlistPath, banlistName, serverIp, dashboardUrl, dashboardToken (either directly or as first line of text file), participants (either json or txt file)\n" +
						"participants format: json: single array named participants containing the names; txt: One name per line");
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
							JsonThing obj = JsonConvert.DeserializeObject<JsonThing>(File.ReadAllText(kv[1]));
							participants = obj.participants;
						}
						else if(kv[1].EndsWith(".txt", StringComparison.Ordinal))
						{
							participants = File.ReadAllLines(kv[1]);
						}
						break;
					case "serverIp":
						serverIp = kv[1];
						break;
					case "dashboardUrl":
						dashboardUrl = kv[1];
						break;
					case "dashboardToken":
						dashboardToken = File.Exists(kv[1]) ? File.ReadAllLines(kv[1])[0] : kv[1];
						break;
				}
			}
			if(participants == null)
			{
				Console.WriteLine("no participants list supplied");
				return;
			}
			Tournament t = new Tournament(participants, 4, serverIp, dashboardUrl, dashboardToken);
			do
			{
				Console.WriteLine(t);
				Console.WriteLine("Press any key to initiate the next round");
				Console.ReadKey(true);
			}
			while (t.ProcessRound());
		}
	}
}
