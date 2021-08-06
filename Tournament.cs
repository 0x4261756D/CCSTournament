using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CCSTournament
{
	public class Tournament
	{
		string[] participants;

		List<Dictionary<int, int>> groups;

		int  groupSize;

		List<List<int>> numbers;
		public string ip;

		public Tournament(string[] participants, int initialGroupSize = 4, string ip = "127.0.0.1")
		{
			this.participants = participants;
			this.ip = ip;
			groupSize = initialGroupSize;
			groups = new List<Dictionary<int, int>>();
			for(int i = 0; i < Math.Ceiling((double)participants.Length / groupSize); i++)
			{
				groups.Add(new Dictionary<int, int>());
				for(int j = 0; j < groupSize; j++)
				{
					if (i * groupSize + j == participants.Length)
						break;
					groups[i].Add(i * groupSize + j, 0);
				}
			}
			SetupRound();
		}

		public void ProcessRound()
		{
			// Check if the tournament is already done
			if (groups.Count <= 1)
				return;
			// Do the matches
			Matches();
			// Sort inside the groups
			// Sort the groups
			// Special case if number of groups is odd
			// Merge two adjacent groups
		}

		private void SetupRound()
		{
			numbers = new List<List<int>>();
			for(int i = 0; i < groupSize-1; i++)
			{
				numbers.Add(new List<int>());
				for (int j = i + 1; j < groupSize; j++)
				{
					numbers[i].Add(j);
				}
			}
			foreach(List<int> l in numbers)
			{
				foreach(int i in l)
				{
					Console.Write(i + " ");
				}
				Console.WriteLine();
			}
		}

		private void Merge(int i, int j)
		{
		}

		//TODO (dummy)
		private void Matches()
		{
			string s = "";
			List<int> indices = new List<int>();
			for(int i = 0; i < numbers.Count; i++)
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
				if(numbers[i].Count == 0)
				{
					indices.Add(i);
				}
			}
			indices.Sort((a, b) => -a.CompareTo(b));
			foreach(int i in indices)
			{
				numbers.RemoveAt(i);
			}
			Console.WriteLine(s);
			List<Room> rooms = new List<Room>();
			for(int i = 0; i < groups.Count; i++)
			{
				for(int j = 0; j < groups[i].Count - 1; j += 2)
				{
					rooms.Add(new Room(participants[groups[i].ElementAt(j).Key], participants[groups[i].ElementAt(j + 1).Key], ip));
				}
			}
			while(rooms.Count > 0)
			{
				var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				for(int i = 0; i < rooms.Count; i++)
				{
					if (!rooms[i].Process())
					{
						rooms[i].Connection.Close();
						//rooms[i] = null;
						rooms.RemoveAt(i);
					}
				}
				Thread.Sleep(Math.Max(30 - (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - now), 1));
			}
		}

		public override string ToString()
		{
			string s = "";
			foreach(Dictionary<int, int> group in groups)
			{
				s += "GROUP starts\n";
				foreach(KeyValuePair<int, int> player in group)
				{
					s += $"{participants[player.Key]}: {player.Value}\n";
				}
				s += "GROUP ends\n";
			}
			return s;
		}
	}
}
