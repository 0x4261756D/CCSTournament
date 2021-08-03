using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CCSTournament
{
	public class Tournament
	{
		string[] participants;

		Dictionary<int, int> scores;

		int round, groupSize;

		List<List<int>> numbers;

		public Tournament(string[] participants, int initialGroupSize = 4)
		{
			this.participants = participants;
			groupSize = initialGroupSize;
			SetupRound();
		}

		public void ProcessRound()
		{
			// Check if the tournament is already done
			// Do the matches
			Matches();
			// Sort inside the groups
			// Sort the groups
			// Special case if number of groups is odd
			// Merge two adjacent groups
		}

		void SetupRound()
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

		void Merge(int i, int j)
		{
		}
		//TODO (dummy)
		void Matches()
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
		}

		public override string ToString()
		{
			return "";
		}
	}
}
