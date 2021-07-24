using System;
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

namespace serverCreator
{
    class MainClass
    {
        public static YGOClient Client { get; set; }
        public static uint  banlist_hash = 0,
                            startingLP = 8000,
                            handshake_magic = 4043399681;
        public static byte allowed = (byte)DuelAllowedCards.ANY,
                            mode = 0,
                            duelRule = 0,
                            dontCheckDeck = 0,
                            dontShuffleDeck = 0,
                            startingDrawCount = 5,
                            drawCountPerTurn = 1; 
        public static ushort timeLimitInSeconds = 300;
        public static ulong duelFlags = 190464,
							extraRules = 0;
        public static int 	team0Count = 1,
							team1Count = 1,
							bestOf = 3,
							forb = 0,
							version = 39 | 1 << 8 | 9 << 16,
							host_port = 7911;
        public static string 	username = "test",
								notes = "Hello, World!",
								host_info = "This is a hostinfo",
								host_address = "85.214.233.223";

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
            Client = new YGOClient();
            Client.Connected += OnConnected;
			Client.PacketReceived += OnPacketReceived;
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
			Client.Connect(address, host_port);
			while (Client.IsConnected)
			{
				Client.Update();
				Console.WriteLine(Client.IsConnected);
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
					Console.WriteLine($"Expected {expected_version}, got {version}");
					break;
			}
			Client.Close();
		}

		private static void OnConnected()
        {
			Console.WriteLine("In OnConnected");
			BinaryWriter packet = GamePacketFactory.Create(CtosMessage.PlayerInfo);
            packet.WriteUnicode(username, 20);
			Client.Send(packet);
            byte[] padding2 = { 0xaa, 0xbb }, unused = { 0x00, 0x00}, padding3 = { 0xaa, 0xbb, 0xcc};
			packet = GamePacketFactory.Create(CtosMessage.CreateGame);
			//hostinfo
			packet.Write(banlist_hash);
            packet.Write(allowed);
			//packet.Write(mode);
			//packet.Write(duelRule);
			packet.Write(unused);
            packet.Write(dontCheckDeck);
            packet.Write(dontShuffleDeck);
            packet.Write(padding3);
            packet.Write(startingLP);
            packet.Write(startingDrawCount);
            packet.Write(drawCountPerTurn);
            packet.Write(timeLimitInSeconds);
            packet.Write((uint)((duelFlags) >> 32) & 0xFFFFFFFF);
            packet.Write(handshake_magic);
            packet.Write(version);
            packet.Write(team0Count);
            packet.Write(team1Count);
            packet.Write(bestOf);
            packet.Write((uint)((duelFlags) & 0xFFFFFFFF));
            packet.Write(forb);
            packet.Write(extraRules);
            packet.Write(padding2);
            packet.WriteUnicode("UNUSED", 20);
			packet.WriteUnicode(host_info, 20);
			try
			{
				byte[] content = Encoding.UTF8.GetBytes(notes + "\0");
				if (content.Length > MAX_NOTES_LENGTH)
					throw new Exception("Message too long");
				packet.Write(content);
				for (int i = MAX_NOTES_LENGTH - content.Length; i > 0; i--)
					packet.Write((byte)0);
			}
			catch(Exception e)
			{
				Console.WriteLine("Warning, unable to encode game notes, sending empyt string instead\n" + e);
				for(int i = 0; i < (MAX_NOTES_LENGTH / 8); i++)
				{
					packet.Write((ulong)0);
				}
			}
			Client.Send(packet);
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
}
