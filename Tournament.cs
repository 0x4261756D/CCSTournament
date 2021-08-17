using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;

namespace CCSTournament
{
	public class Tournament
	{
		readonly string[] participants;

		List<Dictionary<int, int>> groups;
		int groupSize;

		List<List<int>> numbers;
		public string ip, dashboard;
		private string dashboardToken;
		public int mergeRounds;

		public Tournament(string[] participants, int initialGroupSize, string ip, string dashboard, string dashboardToken, int mergeRounds)
		{
			this.participants = participants;
			this.ip = ip;
			this.dashboard = dashboard;
			this.dashboardToken = dashboardToken;
			if(mergeRounds > initialGroupSize)
			{
				Console.WriteLine("too many mergeRounds");
			}
			this.mergeRounds = mergeRounds;
			groupSize = initialGroupSize;
			groups = new List<Dictionary<int, int>>();
			for (int i = 0; i < Math.Ceiling((double)participants.Length / groupSize); i++)
			{
				groups.Add(new Dictionary<int, int>());
				for (int j = 0; j < groupSize; j++)
				{
					if (i * groupSize + j == participants.Length)
						break;
					groups[i].Add(i * groupSize + j, 0);
				}
			}
		}

		public void RoundRobinPhase()
		{
			SetupRound();
			// Do the matches
			while (Matches())
			{
				MainClass.WaitAndNotify("Press any key to initiate the next matches");
			}
		}
		public bool MergingPhase()
		{
			// Check if the tournament is already done
			if (groups.Count <= 1)
				return false;
			// Sort the groups
			groups.Sort((a, b) => a.Count.CompareTo(b.Count));
			// Special case if number of groups is odd
			if((groups.Count & 1) != 0)
				Merge(0, 1);
			// Merge two adjacent groups
			List<int> l = new List<int>();
			for(int i = 0; i < groupSize; i++)
			{
				l.Add(i);
				l.Add(i);
			}
			HandleRooms(l, true);
			int merges = groups.Count / 2 - 1;
			for (int i = 0; i < merges; i++)
			{
				Merge(i, i + 1);
			}
			groupSize *= 2;
			return true;
		}

		private void SetupRound()
		{
			numbers = new List<List<int>>();
			for (int i = 0; i < groupSize - 1; i++)
			{
				numbers.Add(new List<int>());
				for (int j = i + 1; j < groupSize; j++)
				{
					numbers[i].Add(j);
				}
			}
			foreach (List<int> l in numbers)
			{
				foreach (int i in l)
				{
					Console.Write(i + " ");
				}
				Console.WriteLine();
			}
		}

		private List<int> Permute()
		{
			string s = "";
			List<int> indices = new List<int>();
			for (int i = 0; i < numbers.Count; i++)
			{
				if (s.Contains(i.ToString()))
					continue;
				int j = 0;
				while (s.Contains(numbers[i][j].ToString()))
				{
					j++;
				}
				s += i.ToString() + numbers[i][j];
				numbers[i].RemoveAt(j);
				if (numbers[i].Count == 0)
				{
					indices.Add(i);
				}
			}
			indices.Sort((a, b) => -a.CompareTo(b));
			foreach (int i in indices)
			{
				numbers.RemoveAt(i);
			}
			return s.ToList().ConvertAll(x => Convert.ToInt32(x.ToString()));
		}

		private bool Matches()
		{
			List<int> l = Permute();
			if (l.Count == 0) return false;
			HandleRooms(l);
			return true;
		}

		private void Merge(int i, int j)
		{
			foreach (var s in groups[j])
			{
				groups[i].Add(s.Key, s.Value);
			}
			groups.RemoveAt(j);
		}

		private void HandleRooms(List<int> indices, bool merge = false)
		{
			List<Room> rooms = new List<Room>();
			//HACK HACK HACK HACK
			if (merge)
			{
				for(int i = 0; i < groups.Count - 1; i += 2)
				{
					for(int j = 0; j < groups[i].Count - 1; j += 2)
					{
						// Handle unusually small group size
						if (indices[j] > groups[i].Count) continue; 
						rooms.Add(new Room(participants[groups[i].OrderByDescending(x => x.Value).ElementAt(indices[j]).Key],
										participants[groups[i + 1].OrderByDescending(x => x.Value).ElementAt(indices[j + 1]).Key], ip, bestOf: 3));
					}
					// Handle odd group size
					if ((groups[i].Count & 1) != 0)
					{
						groups[i][groups[i].Count - 1] += 1;
					}
				}
			}
			else
			{
				for (int i = 0; i < groups.Count; i++)
				{
					for (int j = 0; j < groups[i].Count - 1; j += 2)
					{
						// Handle unusually small group size
						if (indices[j] > groups[i].Count) continue;
						rooms.Add(new Room(participants[groups[i].ElementAt(indices[j]).Key],
										participants[groups[i].ElementAt(indices[j + 1]).Key], ip, bestOf: 3));
					}
					// Handle odd group size
					if((groups[i].Count & 1) != 0)
					{
						groups[i][groups[i].Count - 1] += 1;
					}
				}
			}
			while (rooms.Count > 0)
			{
				var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				for (int i = 0; i < rooms.Count; i++)
				{
					int[] scores = new int[2];
					if (!rooms[i].Process(out scores))
					{
						for (int j = 0; j < scores.Length; j++)
						{
							int index = Array.IndexOf(participants, rooms[i].ps[j]);
							for (int k = 0; k < groups.Count; k++)
							{
								if (groups[k].ContainsKey(index))
								{
									groups[k][index] += scores[j];
								}
							}
						}
						rooms[i].Connection.Close();
						rooms.RemoveAt(i);
						string s = JsonConvert.SerializeObject(CreateJsonRepresentation());
						using (var httpClient = new HttpClient())
						{
							using (var request = new HttpRequestMessage(new HttpMethod("POST"), dashboard))
							{
								request.Content = new StringContent(s);
								request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
								request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36");

								var response = httpClient.SendAsync(request);
								response.Wait();
								Console.WriteLine(response.Result.StatusCode);
							}
						}
					}
				}
				Thread.Sleep(Math.Max(30 - (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - now), 1));
			}
		}

		public override string ToString()
		{
			string s = "";
			foreach (Dictionary<int, int> g in groups)
			{
				s += "GROUP starts\n";
				foreach (KeyValuePair<int, int> player in g.OrderByDescending(x => x.Value))
				{
					s += $"{participants[player.Key]}: {player.Value}\n";
				}
				s += "GROUP ends\n";
			}
			return s;
		}

		#region JSON creation stuff
		private payload CreateJsonRepresentation()
		{
			// HACK Please don't tell anybody of the crimes I commited in here
			return new payload
			{
				tournament = new tournament
				{
					groups = groups.ConvertAll((input) =>
					{
						group g = new group
						{
							participants = input.OrderByDescending(x => x.Value).ToList().ConvertAll((x) => new participant
							{
								name = participants[x.Key],
								score = x.Value
							}).ToArray()
						};
						return g;
					}).ToArray()
				},
				token = dashboardToken
			};
		}

		private struct payload
		{
			public tournament tournament;
			public string token;
		}

		private struct tournament
		{
			public group[] groups;
		}

		private struct group
		{
			public participant[] participants;
		}

		private struct participant
		{
			public string name;
			public int score;
		}
		#endregion JSON creation stuff
	}
}
