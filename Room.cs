using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using YGOSharp.Network;
using YGOSharp.Network.Enums;
using YGOSharp.Network.Utils;

namespace CCSTournament
{
	public class Room
	{
		public YGOClient Connection { get; set; }
		public static uint banlistHash { get; set; } = 0;
		public static string banlistPath { get; set; } = "";
		public static string banlistName { get; set; } = "";
		public byte allowed { get; set; } = 3;
		public bool dontCheckDeck { get; set; } = false;
		public bool dontShuffleDeck { get; set; } = false;
		public uint startingLP { get; set; } = 8000;
		public byte startingDrawCount { get; set; } = 5;
		public byte drawCountPerTurn { get; set; } = 1;
		public ushort timeLimitInSeconds { get; set; } = 300;
		public ulong duelFlags { get; set; } = 190464;
		public int t0Count { get; set; } = 1;
		public int t1Count { get; set; } = 1;
		public int bestOf { get; set; } = 1;
		public int forb { get; set; } = 0;
		public ushort extraRules { get; set; } = 0;
		public string notes { get; set; } = "";
		public int Version { get; set; } = 39 | 1 << 8 | 9 << 16;

		public string host_address;
		public int host_port = 7911;
		public string Username { get; set; } = "Tournament";
		public string HostInfo { get; set; } = "";

		const int MAX_NOTES_LENGTH = 200;
		private int[] ready = new int[2], scores = new int[2];
		public string[] ps, names = new string[2];
		private bool sw;

		public enum DuelAllowedCards
		{
			OCG_ONLY,
			TCG_ONLY,
			OCG_TCG,
			WITH_PRERELEASE,
			ANY
		}

		public Room(string p1 = "", string p2 = "", string ip = "127.0.0.1", string password = "")
		{
			banlistHash = Banlist.ParseForBanlists(banlistPath, banlistName);
			notes = p1 + " vs. " + p2;
			ps = new string[] { p1, p2 };
			host_address = ip;
			HostInfo = password;
			Connection = new YGOClient();
			Connection.Connected += OnConnected;
			Connection.PacketReceived += OnPacketReceived;
			Connection.Disconnected += OnDisconnected;
			IPAddress address;
			try
			{
				address = IPAddress.Parse(host_address);
			}
			catch (Exception)
			{
				IPHostEntry entry = Dns.GetHostEntry(host_address);
				address = entry.AddressList.FirstOrDefault(findIPv4 => findIPv4.AddressFamily == AddressFamily.InterNetwork);
			}
			Connection.Connect(address, host_port);
		}

		private void OnDisconnected(Exception obj)
		{
			Console.WriteLine($"{notes} was disconnected. Reason: {obj}");
		}

		public bool Process(out int[] results)
		{
			if (Connection.IsConnected)
			{
				Connection.Update();
			}
			if (!Connection.IsConnected)
				results = scores;
			else
				results = null;
			return Connection.IsConnected;
		}

		private void OnPacketReceived(BinaryReader packet)
		{
			StocMessage id = (StocMessage)packet.ReadByte();
			Console.WriteLine("StoC Message: " + id.ToString());
			switch (id)
			{
				case StocMessage.ErrorMsg:
					OnErrorMsg(packet);
					break;
				case StocMessage.GameMsg:
					OnGameMsg(packet);
					break;
				case StocMessage.Chat:
					OnChatMsg(packet);
					break;
				case StocMessage.Chat_2:
					OnChat2Msg(packet);
					break;
				case StocMessage.HsPlayerEnter:
					OnPlayerEnter(packet);
					break;
				case StocMessage.HsPlayerChange:
					OnPlayerChange(packet);
					break;
				case StocMessage.LeaveGame:
					Connection.Close();
					break;
			}
		}

		private void OnPlayerChange(BinaryReader packet)
		{
			int change = packet.ReadByte();
			int pos = (change >> 4) & 0xf;
			pos = sw ? pos : (1 - pos);
			int state = change & 0xf;
			if(state == (int)PlayerChange.Ready)
			{
				ready[pos] |= 2;
				if (ready[0] == 3 && ready[1] == 3)
					StartGame();
			}
			else if(state == (int)PlayerChange.NotReady)
			{
				ready[pos] &= ~2;
			}
			
		}

		private void OnPlayerEnter(BinaryReader packet)
		{
			string name = packet.ReadUnicode(20);
			int pos = packet.ReadByte();
			names[pos] = name;
			sw = name == ps[1 - pos];
		}

		private void OnChatMsg(BinaryReader packet)
		{
			int player = packet.ReadInt16();
			string message = packet.ReadUnicode(256);
			HandleChat(message, player);
		}

		private void OnChat2Msg(BinaryReader packet)
		{
			int playerType = packet.ReadByte();
			if(playerType == 0)
			{
				int isTeam = packet.ReadByte();
				string name = packet.ReadUnicode(20);
				string message = packet.ReadUnicode(256);
				HandleChat(message, Array.IndexOf(names, name));
			}
		}

		private void HandleChat(string message, int player)
		{
			switch (message.ToLower())
			{
				case "!ready":
					player = sw ? player : (1 - player);
					//lowest bit for Chat ready
					ready[player] ^= 1;
					if (ready[0] == 3 && ready[1] == 3)
					{
						StartGame();
					}
					break;
				case "!p1":
					sw = player == 1;
					break;
			}
		}

		private void StartGame()
		{
			BinaryWriter packet = GamePacketFactory.Create(CtosMessage.HsStart);
			Connection.Send(packet);
		}

		private void OnGameMsg(BinaryReader packet)
		{
			int msg = packet.ReadByte();
			Console.WriteLine("Game Message received with " + msg);
			if(msg == 5)
			{
				int winner = packet.ReadByte();
				int reason = packet.ReadByte();
				scores[sw ? (1 - winner) : winner]++;
			}
		}

		private void OnErrorMsg(BinaryReader packet)
		{
			int msg = packet.ReadByte();
			for (int i = 0; i < 3; i++) // align
				packet.ReadByte();
			switch (msg)
			{
				case 2: // ERRMSG_DECKERROR
					int flag = packet.ReadInt32();
					int got = packet.ReadInt32();
					int min = packet.ReadInt32();
					int max = packet.ReadInt32();
					int code = packet.ReadInt32();
					Console.WriteLine($"Something wrong with the deck, flag: {flag}, code: {code}, got: {got}, min: {min}, max: {max}");
					break;
				case 5:
					int expected_version = packet.ReadInt32();
					Console.WriteLine($"Expected {expected_version}, got {Version}");
					break;
			}
			Connection.Close();
		}

		private void OnConnected()
		{
			BinaryWriter packet = GamePacketFactory.Create(CtosMessage.PlayerInfo);
			packet.WriteUnicode(Username, 20);
			Connection.Send(packet);
			byte[] padding2 = { 0xAA, 0xBB };
			byte[] unused = { 0x00, 0x00 };
			byte[] padding3 = { 0xAA, 0xBB, 0xCC };
			packet = GamePacketFactory.Create(CtosMessage.CreateGame);
			// hostInfo
			packet.Write(banlistHash);
			packet.Write(allowed);
			packet.Write(unused); // mode & duelRule
			packet.Write((byte)(dontCheckDeck ? 1 : 0));
			packet.Write((byte)(dontShuffleDeck ? 1 : 0));
			packet.Write(padding3);
			packet.Write(startingLP);
			packet.Write(startingDrawCount);
			packet.Write(drawCountPerTurn);
			packet.Write(timeLimitInSeconds);
			packet.Write((uint)((duelFlags >> 32) & 0xFFFFFFFF));
			packet.Write((uint)4043399681); // handshake
			packet.Write(Version); // version
			packet.Write(t0Count);
			packet.Write(t1Count);
			packet.Write(bestOf);
			packet.Write((uint)(duelFlags & 0xFFFFFFFF));
			packet.Write(forb);
			packet.Write(extraRules);
			packet.Write(padding2);
			// name
			packet.WriteUnicode("", 20); // UNUSED
										 // pass
			packet.WriteUnicode(HostInfo, 20);
			// notes
			try
			{
				// Write notes in UTF8 format making sure to always write exactly
				// MAX_NOTES_LENGTH bytes.
				byte[] content = Encoding.UTF8.GetBytes(notes + "\0");
				if (content.Length > MAX_NOTES_LENGTH)
					throw new Exception();
				packet.Write(content);
				for (int i = MAX_NOTES_LENGTH - content.Length; i > 0; i--)
					packet.Write((byte)0);
			}
			catch (Exception)
			{
				Console.WriteLine("Warning: Unable to encode CreateGame.notes, sending empty string instead.");
				for (int i = 0; i < (MAX_NOTES_LENGTH / 8); i++) packet.Write((ulong)0);
			}
			Connection.Send(packet);
			packet = GamePacketFactory.Create(CtosMessage.HsToObserver);
			Connection.Send(packet);
		}

		public static class GamePacketFactory
		{
			public static BinaryWriter Create(CtosMessage message)
			{
				BinaryWriter writer = new BinaryWriter(new MemoryStream());
				writer.Write((byte)message);
				return writer;
			}
		}

		public static class Banlist
		{
			const uint BANLIST_HASH_MAGIC = 0x7DFCEE6A;

			public static uint Salt(uint hash, uint code, int count)
			{
				int HASH_MAGIC_1 = 18, HASH_MAGIC_2 = 14, HASH_MAGIC_3 = 27, HASH_MAGIC_4 = 5;
				return hash ^ ((code << HASH_MAGIC_1) | (code >> HASH_MAGIC_2)) ^ ((code << (HASH_MAGIC_3 + count)) | (code >> (HASH_MAGIC_4 - count)));
			}

			public static uint ParseForBanlists(string filename, string banlistName)
			{
				uint hash = BANLIST_HASH_MAGIC;
				if (!File.Exists(filename)) return 0;
				string[] lines = File.ReadAllLines(filename);
				bool doAnything = false;
				foreach (string line in lines)
				{

					if (line.StartsWith("!", StringComparison.Ordinal))
					{
						if (!doAnything)
							doAnything = line.Contains(banlistName);
						else
							return hash;
					}
					else if (doAnything && "0123456789".Contains(line[0]))
					{
						string[] p = line.Split(' ');
						uint code = Convert.ToUInt32(p[0]);
						if (code == 0) throw new Exception("Code can't be 0");
						int count = Convert.ToInt32(p[1]);
						hash = Salt(hash, code, count);
					}
				}
				return (hash == BANLIST_HASH_MAGIC) ? 0 : hash;
			}
		}
	}
}
