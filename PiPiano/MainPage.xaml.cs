//#define DEBUGMEM
//#define RESTARTTIMER

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
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PiPiano
{
    /// <summary>
    /// Main user interface and logic.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Number of sounds
        /// </summary>
        private const int NUM_OF_SOUNDS = 12;

        /// <summary>
        /// Measure timer interval
        /// </summary>
        private const int interval = 40;

        /// <summary>
        /// Values read timer
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// Restart timer
        /// </summary>
        private DispatcherTimer restartTimer;

        /// <summary>
        /// Piano states
        /// </summary>
        private ItemState[] downStates = new ItemState[NUM_OF_SOUNDS];

        /// <summary>
        /// Sound players
        /// </summary>
        private MediaPlayer[] players = new MediaPlayer[NUM_OF_SOUNDS];

        /// <summary>
        /// Is first run
        /// </summary>
        private bool firstTime = true;

        /// <summary>
        /// Line 1 reading
        /// </summary>
        private SpiDevice ADC;

        /// <summary>
        /// Line 2 reading
        /// </summary>
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

#if RESTARTTIMER
            if (gpio != null)
            {
                restartTimer = new DispatcherTimer();
                restartTimer.Interval = TimeSpan.FromMinutes(15);
                restartTimer.Tick += Restart_Timer_Tick;
                restartTimer.Start();
            }
#endif  
        }

        /// <summary>
        /// Restart timer handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Restart_Timer_Tick(object sender, object e)
        {
            ShutdownManager.BeginShutdown(ShutdownKind.Restart, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Initialize players
        /// </summary>
        private void InitPlayers()
        {
            for (int i = 0; i < NUM_OF_SOUNDS; i++)
            {
                players[i] = new MediaPlayer();
                players[i].Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/sound{i}.mp3"));
                players[i].AutoPlay = false;
                downStates[i] = new ItemState();

                switch (i)
                {
                    case 5:
                        downStates[i].MinPressedValue = 110;
                        break;
                    default:
                        downStates[i].MinPressedValue = 100;
                        break;
                }
            }
        }

        /// <summary>
        /// Initialize reading lines
        /// </summary>
        /// <returns></returns>
        private async Task InitSpi()
        {
            if(firstTime)
            {
                firstTime = false;

                timer.Interval = TimeSpan.FromMilliseconds(interval);

                for (int i = 0; i < NUM_OF_SOUNDS; i++)
                {
                    PlayNote(i);
                    await Task.Delay(250);
                }
                if (ADC == null)
                {
                    ADC = await InitSPI(0);
                }

                if (ADC2 == null)
                {
                    ADC2 = await InitSPI(1);
                }

                timer.Start();
            }
        }

        /// <summary>
        /// Initialize SPI
        /// </summary>
        /// <param name="busNumber"></param>
        /// <returns></returns>
        private async Task<SpiDevice> InitSPI(int busNumber)
        {
            var settings = new SpiConnectionSettings(0)                         // Chip Select line
            {
                ClockFrequency = 3600000,                                    // Don't exceed 3.6 MHz
                Mode = SpiMode.Mode0,
            };

            string spiAqs = SpiDevice.GetDeviceSelector(); // ($"SPI{busNimber}");                /* Find the selector string for the SPI bus controller          */
            var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);     /* Find the SPI bus controller device with our selector string  */
            SpiDevice spiDevice = await SpiDevice.FromIdAsync(devicesInfo[busNumber].Id, settings);     /* Create an SpiDevice with our bus controller and SPI settings */
            return spiDevice;
        }

        /// <summary>
        /// Handle timer tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Timer_Tick(object sender, object e)
        {
            timer.Stop();

            await InitSpi();

            ReadSpiData();

            timer.Start();
        }

#if DEBUGMEM
        static int counter = 0;
#endif

        /// <summary>
        /// Read values
        /// </summary>
        private void ReadSpiData()
        {
            try
            {
                // Reading 8 values from SPI0 and play note if state is pressed
                for (int i = 0; i < 8 && ADC != null; i++)
                {
                    int value = ReadAdc(i, ADC);
                    //Trace.WriteLine(i + " = [" + value + " ], ");

                    ItemState state = downStates[i];
                    bool wasPressed = state.IsPressed;
                
                    if (state.UpdateState(value) && !wasPressed)
                    {
                        PlayNote(i);
                    }
                }

                // Reading 8 values from SPI1 and play note if state is pressed
                for (int i = 0; i < 4 && ADC2 != null; i++)
                {
                    int value = ReadAdc(i, ADC2);
                    //Trace.WriteLine(i + 8 + " = [" + value + " ], ");

                    ItemState state = downStates[i + 8];
                    bool wasPressed = state.IsPressed;

                    if (state.UpdateState(value) && !wasPressed)
                    {
                        PlayNote(i + 8);
                    }
                }
            }
            catch (Exception ex)
            {
                //Trace.WriteLine(ex);
            }
            finally
            {
                //Trace.WriteLine("");
            }
        }

        /// <summary>
        /// Read specific channel value
        /// </summary>
        /// <param name="adc_number"></param>
        /// <param name="spiADC"></param>
        /// <returns></returns>
        private static int ReadAdc(int adc_number, SpiDevice spiADC)
        {
            byte byte1 = (byte)1;
            byte byte2 = GetRequestChannelByte(adc_number);

            byte[] request = new byte[3] { byte1, byte2, 0x0 };
            byte[] response = new byte[3];

            spiADC.TransferFullDuplex(request, response);

            int value = ConverToInt(response);

#if DEBUGMEM
            counter++;
            if (counter > 5)
                counter = -5;

            if (counter > 0)
            {
                value = 120;
            }
            else
            {
                value = 90;
            }

#endif

            return value;
        }

        /// <summary>
        /// Get configuration byte for reading channel
        /// </summary>
        /// <param name="adc_number"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Converts result byte data to integer value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int ConverToInt(byte[] data)
        {
            int result = 0;
            result = data[1] & 0x03;
            result <<= 8;
            result += data[2];
            return result;
        }

        /// <summary>
        /// Plays note by index
        /// </summary>
        /// <param name="index"></param>
        private void PlayNote(int index)
        {
            MediaPlayer mp = players[index];
            if (mp.PlaybackSession.CanPause)
            {
                mp.Pause();
                mp.PlaybackSession.Position = TimeSpan.FromMilliseconds(0);
            }

            mp.Play();
        }

        /// <summary>
        /// Handles piano button ckick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PlayNote(int.Parse((e.OriginalSource as Button).Tag.ToString()));
        }
    }
}
