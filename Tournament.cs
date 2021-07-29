using System;
using System.Collections.Generic;

namespace serverCreator
{
	public class Tournament
	{
		string[] participants;

		List<List<KVP<string, int>>> groups;

		int round;

		struct KVP<T1, T2>
		{
			public T1 Key;
			public T2 Value;
		}

		public Tournament(string[] participants, int initialGroupSize = 4)
		{
			this.participants = participants;
			groups = new List<List<KVP<string, int>>>();
			for(int i = 0; i < participants.Length; i += initialGroupSize)
			{
				List<KVP<string, int>> g = new List<KVP<string, int>>();
				for (int j = 0; j < initialGroupSize; j++)
				{
					if (i + j == participants.Length)
						break;
					g.Add(new KVP<string, int> { Key = participants[i + j], Value = 0 });
				}
				groups.Add(g);
			}
		}

		public void ProcessRound()
		{
			if (groups.Count <= 1)
			{
				return;
			}
			// Do the matches
			Matches();
			// Sort inside the groups
			for (int i = 0; i < groups.Count; i++)
			{
				groups[i].Sort(PlayerCompare());
			}
			// Sort the groups
			groups.Sort(GroupComare());
			// Merge groups
			// Special case if number of groups is odd
			if ((groups.Count & 1) != 0)
			{
				Merge(1, 2);
			}
			for(int i = 0; i < groups.Count; i ++)
			{
				Merge(i, i + 1);
			}
		}

		private static Comparison<List<KVP<string, int>>> GroupComare()
		{
			return (a, b) => a.Count - b.Count;
		}
		private static Comparison<KVP<string, int>> PlayerCompare()
		{
			return (a, b) => b.Value - a.Value;
		}

		void Merge(int i, int j)
		{
			groups[i].AddRange(groups[j]);
			groups[i].Sort(PlayerCompare());
			groups.RemoveAt(j);
		}

		void Matches()
		{
			Random r = new Random();
			for(int i = 0; i < groups.Count; i++)
			{
				for(int j = 0; j < groups[i].Count; j++)
				{
					groups[i][j] = new KVP<string, int> { Key = groups[i][j].Key, Value = groups[i][j].Value + r.Next(5) };
				}
			}
		}

		public override string ToString()
		{
			string ret = "";
			foreach(List<KVP<string, int>> g in groups)
			{
				ret += "BEGINNING\n";
				foreach(var p in g)
				{
					ret += $"{p.Key}: {p.Value}\n";
				}
				ret += "END\n";
			}
			return ret;
		}
	}
}
