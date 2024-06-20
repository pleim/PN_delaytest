using PacketDotNet;
using PN_DelayWPF.Properties;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PN_DelayWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static LibPcapLiveDevice device1, device2;
        static System.Timers.Timer timer = new(1000), timerGap = new(100);
        static int cnt1, cnt2, interval;
        //static bool gap = false;
        static DateTime dt1, dt2;
        static TimeSpan ts1, ts2;

        public MainWindow()
        {
            InitializeComponent();            
            timer.Elapsed += Timer_Elapsed;
            timerGap.AutoReset = false;
            timerGap.Elapsed += TimerGap_Elapsed;
            textBoxFilter1.Text = Settings.Default.Filter1;
            textBoxFilter2.Text = Settings.Default.Filter2;
            buttonIF_Click(this, new RoutedEventArgs());
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            //if (!(device1 as LibPcapLiveDevice).Opened | !(device2 as LibPcapLiveDevice).Opened) { return; }
            Dispatcher.Invoke(() => lblStatusIF1.Content = $"IF1:   {device1.Statistics.ReceivedPackets}");
            Dispatcher.Invoke(() => lblStatusIF2.Content = $"IF2:   {device2.Statistics.ReceivedPackets}");
            Dispatcher.Invoke(() => lblStatusCnt1.Content = $"1>2:   {cnt1}");
            Dispatcher.Invoke(() => lblStatusCnt2.Content = $"1<2:   {cnt2}");
            Dispatcher.Invoke(() => progress1.Value = ts1.Milliseconds);
            Dispatcher.Invoke(() => progress2.Value = ts2.Milliseconds);
            Dispatcher.Invoke(() => lblIntervalCnt.Content = interval);
            interval++;
            short i = 10;
            string stri = "10";
            Dispatcher.Invoke(() => stri = textBoxInterval.Text);
            short.TryParse(stri, out i);
            if (interval > i) 
            {
                interval = 0;                
                timerGap.Start();
                Dispatcher.Invoke(() => rectGap.Fill = Brushes.Blue);
            }
        }

        private void TimerGap_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {            
            Dispatcher.Invoke(() => rectGap.Fill = null);
        }

        private void buttonIF_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;
            comboBoxIF1.Items.Clear();
            comboBoxIF2.Items.Clear(); 
            foreach (var device in devices)
            {
                comboBoxIF1.Items.Add(device.Description);
                comboBoxIF2.Items.Add(device.Description);
            }
            if(devices.Count > 0) { comboBoxIF1.IsEnabled = true; comboBoxIF2.IsEnabled = true;}

            ILiveDevice? i1 = devices.FirstOrDefault(d => d.Name == Settings.Default.Interface1);
            ILiveDevice? i2 = devices.FirstOrDefault(d => d.Name == Settings.Default.Interface2);
            if(i1 != null && i2 != null)
            {
                Debug.WriteLine(i1.Name + ", " + i2.Name);
                device1 = (LibPcapLiveDevice)i1;
                device2 = (LibPcapLiveDevice)i2;
                comboBoxIF1.SelectedIndex = devices.IndexOf(i1);
                comboBoxIF2.SelectedIndex = devices.IndexOf(i2);
            }
        }

        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            var devices = CaptureDeviceList.Instance;
            device1 = (LibPcapLiveDevice)devices[comboBoxIF1.SelectedIndex];
            device2 = (LibPcapLiveDevice)devices[comboBoxIF2.SelectedIndex];

            device1.OnPacketArrival += Device1_OnPacketArrival;
            device2.OnPacketArrival += Device2_OnPacketArrival;
                        
            device1.Open(DeviceModes.NoCaptureLocal | DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, 1000);
            device2.Open(DeviceModes.NoCaptureLocal | DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, 1000);

            try
            {
                device1.Filter = textBoxFilter1.Text;
                device2.Filter = textBoxFilter2.Text;
            }
            catch (Exception)
            {
                MessageBox.Show("Capture filter error");
                device1?.Close();
                device2?.Close();
                return;
            }

            device1.StartCapture();
            device2.StartCapture();

            buttonClose.IsEnabled = true;
            buttonOpen.IsEnabled = false;
            textBoxFilter1.IsEnabled = false; 
            textBoxFilter2.IsEnabled = false;
            textBoxGap.IsEnabled = false;
            cnt1 = cnt2 = 0;
            short i = 100;
            short.TryParse(textBoxGap.Text, out i);
            timerGap.Interval = i;            
            timer.Start();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            device1?.Close();
            device2?.Close();
            buttonOpen.IsEnabled = true;
            buttonClose.IsEnabled = false;
            textBoxFilter1.IsEnabled = true;
            textBoxFilter2.IsEnabled = true;
            textBoxGap.IsEnabled = true;
        }

        private void comboBoxIF_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboBoxIF1.SelectedIndex > -1 & comboBoxIF2.SelectedIndex > -1) { buttonOpen.IsEnabled = true; }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Filter1 = textBoxFilter1.Text;
            Settings.Default.Filter2 = textBoxFilter2.Text;
            if (device1 != null) Settings.Default.Interface1 = device1.Name;
            if (device2 != null) Settings.Default.Interface2 = device2.Name;
            Settings.Default.Save();
        }

        private void Device1_OnPacketArrival(object sender, PacketCapture e)
        {
            ts1 = DateTime.Now - dt1;
            dt1 = DateTime.Now;
            var map = checkBox1.Dispatcher.Invoke(() => checkBox1.IsChecked);
            //if (map == true)
            if (map == true & !timerGap.Enabled)
            {
                device2?.SendPacket(e.Data);
                cnt1++;
            }
        }

        private void Device2_OnPacketArrival(object sender, PacketCapture e)
        {
            ts2 = DateTime.Now - dt2;
            dt2 = DateTime.Now;
            var map = checkBox2.Dispatcher.Invoke(() => checkBox2.IsChecked);
            if (map == true)
            {
                device1?.SendPacket(e.Data);
                cnt2++;
            }
        }
    }
}