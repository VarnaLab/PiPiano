using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiPiano
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer timer;

        private const int NUM_OF_SOUNDS = 12;
        private ItemState[] downStates = new ItemState[NUM_OF_SOUNDS];
        private MediaPlayer[] players = new MediaPlayer[NUM_OF_SOUNDS];

        private SpiDevice ADC;
        private SpiDevice ADC2;

        public MainPage()
        {
            this.InitializeComponent();

            InitPlayers();

            var gpio = GpioController.GetDefault();
            if (gpio != null)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void InitPlayers()
        {
            for (int i = 0; i < NUM_OF_SOUNDS; i++)
            {
                players[i] = new MediaPlayer();
                players[i].Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/sound{i}.mp3"));
                downStates[i] = new ItemState();
            }
        }

        private async Task InitSpi()
        {
            if (ADC == null)
            {
                ADC = await InitSPI(0);
            }

            if (ADC2 == null)
            {
                ADC2 = await InitSPI(1);
                timer.Interval = TimeSpan.FromMilliseconds(200);
            }
        }

        private async Task<SpiDevice> InitSPI(int busNimber)
        {
            var settings = new SpiConnectionSettings(0)                         // Chip Select line
            {
                ClockFrequency = 3600000,                                    // Don't exceed 3.6 MHz
                Mode = SpiMode.Mode0,
            };

            string spiAqs = SpiDevice.GetDeviceSelector(); // ($"SPI{busNimber}");                /* Find the selector string for the SPI bus controller          */
            var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);     /* Find the SPI bus controller device with our selector string  */
            SpiDevice spiDevice = await SpiDevice.FromIdAsync(devicesInfo[busNimber].Id, settings);     /* Create an SpiDevice with our bus controller and SPI settings */
            return spiDevice;
        }

        private async void Timer_Tick(object sender, object e)
        {
            await InitSpi();

            ReadSpiData();
        }

        private void ReadSpiData()
        {
            for(int i = 0; i < 8; i++)
            {
                int value = ReadAdc(i, ADC);
                Trace.Write(i + " = [" + value + " ], ");

                ItemState state = downStates[i];
                bool wasPressed = state.IsPressed;
                
                if (state.UpdateState(value) && !wasPressed)
                {
                    playNote(i);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                int value = ReadAdc(i, ADC2);
                Trace.Write(i + 8 + " = [" + value + " ], ");

                ItemState state = downStates[i + 8];
                bool wasPressed = state.IsPressed;

                if (state.UpdateState(value) && !wasPressed)
                {
                    playNote(i + 8);
                }
            }

            Trace.WriteLine("");
        }

        private static int ReadAdc(int adc_number, SpiDevice spiADC)
        {
            byte byte1 = (byte)1;
            byte byte2 = GetRequestChannelByte(adc_number);

            byte[] request = new byte[3] { byte1, byte2, 0x0 };
            byte[] response = new byte[3];

            spiADC.TransferFullDuplex(request, response);

            int value = ConverToInt(response);
            return value;
        }

        private static byte GetRequestChannelByte(int adc_number)
        {
            //(byte)(0b10000000 | ((adc_number & 7) << 4));

            byte byte2 = 0b0000000;

            switch (adc_number)
            {
                case 0:
                    byte2 = 0b0000000;
                    break;
                case 1:
                    byte2 = 0b0010000;
                    break;
                case 2:
                    byte2 = 0b0100000;
                    break;
                case 3:
                    byte2 = 0b0110000;
                    break;
                case 4:
                    byte2 = 0b1000000;
                    break;
                case 5:
                    byte2 = 0b1010000;
                    break;
                case 6:
                    byte2 = 0b1100000;
                    break;
                case 7:
                    byte2 = 0b1110000;
                    break;
            }

            return byte2;
        }

        private static int ConverToInt(byte[] data)
        {
            int result = 0;
            result = data[1] & 0x03;
            result <<= 8;
            result += data[2];
            return result;
        }

        private void playNote(int i)
        {
            if(players[i].PlaybackSession.CanPause)
            {
                players[i].Dispose();
                players[i] = new MediaPlayer();
                players[i].Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/sound{i}.mp3"));
            }

            players[i].Play();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            playNote(int.Parse((e.OriginalSource as Button).Tag.ToString()));
        }
    }
}
