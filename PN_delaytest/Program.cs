using SharpPcap;
using SharpPcap.Npcap;
using System;
using System.Text;

namespace PN_delaytest
{
    class Program
    {
        static NpcapDevice device1, device2;
        static int skip1 = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Capture and resend between two interfaces");
            
            // Print SharpPcap version
            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("SharpPcap {0}", ver);

            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }
            
            Console.WriteLine("\r\nHit a key from 1 to 9 to skip n packets from device 1 -> 2\r\n");
            
            /* Scan the list printing every entry */
            int i = 0;
            foreach (var dev in devices)              
                Console.WriteLine($"{i++}: {dev.Description}");

            Console.WriteLine("\r\nSelect device 1:");
            i = int.Parse(Console.ReadLine());
            device1 = (NpcapDevice)devices[i];
            Console.WriteLine("\r\nSelect device 2:");
            i = int.Parse(Console.ReadLine());
            device2 = (NpcapDevice)devices[i];


            device1.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival1);
            device2.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival2);

            // Open the device for capturing            
            device1.Open(OpenFlags.NoCaptureLocal | OpenFlags.Promiscuous | OpenFlags.MaxResponsiveness, 1000); 
            device2.Open(OpenFlags.NoCaptureLocal | OpenFlags.Promiscuous | OpenFlags.MaxResponsiveness, 1000);
            device1.StartCapture();
            device2.StartCapture();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if(key.Key == ConsoleKey.Escape) break;
                int n = 0;
                int.TryParse(key.KeyChar.ToString(), out n);
                n = n * 3;                                                    // Skip factor
                if(n>0 && n<100) 
                { 
                    skip1 = n;
                    Console.WriteLine("skip " + n);
                }
            }

            device1.StopCapture();
            device2.StopCapture();
            Console.WriteLine();
            Console.WriteLine(device1.Statistics.ToString());
            Console.WriteLine(device2.Statistics.ToString());
            device1.Close();
            device2.Close();
        }

        private static void device_OnPacketArrival1(object sender, CaptureEventArgs e)
        {
            if(skip1==0)
            {
                device2.SendPacket(e.Packet.Data);
                //Console.Write($"1 ({e.Packet.Data.Length}) ");                
            }
            else
            {
                Console.Write($"1 ({e.Packet.Data.Length}) skipped ");
                skip1--;
            }
            //Console.WriteLine($"1->2: {e.Packet.Data.Length:D4} {ByteArrayToString(e.Packet.Data)}");
        }

        private static void device_OnPacketArrival2(object sender, CaptureEventArgs e)
        {
            device1.SendPacket(e.Packet.Data);
            //Console.Write("2 ");
            //Console.Write($"2 ({e.Packet.Data.Length}) ");
            //Console.WriteLine($"2->1: {e.Packet.Data.Length:D4} {ByteArrayToString(e.Packet.Data)}");
        }

        public static string ByteArrayToString(byte[] ba)
        {
            if (ba.Length < 16) return "-";
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            for (int i = 0; i < 14; i++)
            {
                hex.AppendFormat("{0:x2}", ba[i]);
            }            
            return hex.ToString();
        }
    }
}
