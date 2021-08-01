using System;
using System.Collections.Generic;
using System.Linq;

namespace CCSTournament
{
	public class Tournament
	{
		string[] participants;

		Dictionary<int, int> scores;

		int round, groupSize;

		public Tournament(string[] participants, int initialGroupSize = 4)
		{
			this.participants = participants;
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

		void Merge(int i, int j)
		{

		}
		//TODO (dummy)
		void Matches()
		{

		}

		public override string ToString()
		{
			return "";
		}
	}
}
