using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiPiano
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int PB_PIN_START = 5;
        private GpioPin[] pushButtons = new GpioPin[8];
        private bool[] downStates = new bool[8];
        private DispatcherTimer timer;
        private MediaPlayer player = new MediaPlayer();



        public MainPage()
        {
            this.InitializeComponent();

            var gpio = GpioController.GetDefault();
            if (gpio != null)
            {
                InitGPIO(gpio);
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        private void InitGPIO(GpioController gpio)
        {
            for (int i = 0; i < pushButtons.Length; i++)
            {
                pushButtons[i] = gpio.OpenPin(i + PB_PIN_START);
                pushButtons[i].SetDriveMode(GpioPinDriveMode.Input);
                downStates[i] = false;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            CheckButtonState();
        }

        private void CheckButtonState()
        {
            for (int i = 0; i < pushButtons.Length; i++)
            {
                bool isPinOn = pushButtons[i].Read() == GpioPinValue.High;
                if (downStates[i] != isPinOn && isPinOn)
                    playNote(i);
                downStates[i] = isPinOn;
            }
        }

        private void playNote(int i)
        {
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/sound{i}.wav"));
            mediaPlayer.Play();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            playNote(int.Parse((e.OriginalSource as Button).Tag.ToString()));
        }
    }
}
