﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using YGOSharp.Network;
using System.Net.Sockets;
using YGOSharp.Network.Enums;
using YGOSharp.Network.Utils;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace serverCreator
{
    class MainClass
    {
        public static YGOClient Connection { get; set; }
		public static uint banlistHash { get; set; } = 0;
		public static string banlistPath { get; set; } = "";
		public static string banlistName { get; set; } = "";
		public static byte allowed { get; set; } = 3;
		public static bool dontCheckDeck { get; set; } = false;
		public static bool dontShuffleDeck { get; set; } = false;
		public static uint startingLP { get; set; } = 8000;
		public static byte startingDrawCount { get; set; } = 5;
		public static byte drawCountPerTurn { get; set; } = 1;
		public static ushort timeLimitInSeconds { get; set; } = 300;
		public static ulong duelFlags { get; set; } = 190464;
		public static int t0Count { get; set; } = 1;
		public static int t1Count { get; set; } = 1;
		public static int bestOf { get; set; } = 1;
		public static int forb { get; set; } = 0;
		public static ushort extraRules { get; set; } = 0;
		public static string notes { get; set; } = "";
		public static int Version { get; set; } = 39 | 1 << 8 | 9 << 16;

		public static string host_address = "85.214.233.223";
		public static int host_port = 7911;
		public static string Username { get; set; } = "Test";
		public static string HostInfo { get; set; } = "Hostinfo";

		const int MAX_NOTES_LENGTH = 200;

        public enum DuelAllowedCards
        {
            OCG_ONLY,
            TCG_ONLY,
            OCG_TCG,
            WITH_PRERELEASE,
            ANY
        }

        public static void Main(string[] args)
        {
			if (args.Length > 0)
				banlistPath = args[0];
			banlistHash = Banlist.ParseForBanlists(banlistPath, banlistName);
            Connection = new YGOClient();
            Connection.Connected += OnConnected;
			Connection.PacketReceived += OnPacketReceived;
			IPAddress address;
			try
			{
				address = IPAddress.Parse(host_address);
			}
			catch(Exception)
			{
				IPHostEntry entry = Dns.GetHostEntry(host_address);
				address = entry.AddressList.FirstOrDefault(findIPv4 => findIPv4.AddressFamily == AddressFamily.InterNetwork);
			}
			Connection.Connect(address, host_port);
			while (Connection.IsConnected)
			{
				Connection.Update();
				Console.WriteLine(Connection.IsConnected);
				Thread.Sleep(30);
			}
		}

		private static void OnPacketReceived(BinaryReader packet)
		{
			StocMessage id = (StocMessage)packet.ReadByte();
			Console.WriteLine("StoC Message: " + id.ToString());
			switch (id)
			{
				case StocMessage.ErrorMsg:
					OnErrorMsg(packet);
					break;
			}
		}

		private static void OnErrorMsg(BinaryReader packet)
		{
			int msg = packet.ReadByte();
			for (int i = 0; i < 3; i++) // align
				packet.ReadByte();
			switch(msg) 
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

		private static void OnConnected()
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
			foreach(string line in lines)
			{

				if(line.StartsWith("!", StringComparison.Ordinal))
				{
					if (!doAnything)
						doAnything = line.Contains(banlistName);
					else
						return hash;
				}
				else if(doAnything && "0123456789".Contains(line[0]))
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
