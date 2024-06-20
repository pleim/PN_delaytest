using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PN_delaytest
{
    class Program
    {
        static LibPcapLiveDevice device1, device2;
        static int skip = 0;
        static System.Timers.Timer timer1 = new System.Timers.Timer();
        static Random rand = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("Capture and resend between two interfaces");
            
            // Print SharpPcap version
            //string ver = SharpPcap.
            Console.WriteLine("SharpPcap {0}");

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
            device1 = (LibPcapLiveDevice)devices[i];
            Console.WriteLine("\r\nSelect device 2:");
            i = int.Parse(Console.ReadLine());
            device2 = (LibPcapLiveDevice)devices[i];
            Console.WriteLine("\r\nActivate automatic skipping? [y/n]:");
            char enabletimer = 'n';
            enabletimer = Console.ReadLine().First();

            device1.OnPacketArrival += Device1_OnPacketArrival;
            device2.OnPacketArrival += Device2_OnPacketArrival;
            
            // Open the device for capturing            
            device1.Open(DeviceModes.NoCaptureLocal | DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, 1000 );
            device2.Open(DeviceModes.NoCaptureLocal | DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, 1000 );

            device1.StartCapture();
            device2.StartCapture();

            // Timer for timed skip
            timer1.Elapsed += Timer1_Elapsed;
            timer1.Interval = 1000;
            timer1.AutoReset = false;
            if (enabletimer == 'y') timer1.Start();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if(key.Key == ConsoleKey.Escape) break;
                int n = 0;
                int.TryParse(key.KeyChar.ToString(), out n);
                if(n>0 && n<10) 
                { 
                    skip = n;
                    Console.WriteLine("skip " + n);
                }
            }

            timer1.Stop();
            timer1 = null;
            device1.StopCapture();
            device2.StopCapture();
            Console.WriteLine();
            Console.WriteLine(device1.Statistics.ToString());
            Console.WriteLine(device2.Statistics.ToString());
            device1.Close();
            device2.Close();
        }

        private static void Device1_OnPacketArrival(object sender, PacketCapture e)
        {
            if(skip==0)
            {
                device2.SendPacket(e.Data);
            }
            else
            {
                Console.Write($"1 ({e.Data.Length}) skipped ");
                skip--;
            }
        }

        private static void Device2_OnPacketArrival(object sender, PacketCapture e)
        {
            device1.SendPacket(e.Data);
        }

        private static void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int intervall = rand.Next(9000) + 1000;
            int skipcnt = rand.Next(30) + 1;
            skip = skipcnt;
            timer1.Interval = intervall;
            timer1.Start();
            Console.WriteLine($"Timer restarted, {intervall} ms, skip {skipcnt}");
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
