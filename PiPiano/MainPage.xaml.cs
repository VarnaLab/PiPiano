using System;
using Windows.Devices.Gpio;
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
        private const int NUM_OF_SOUNDS = 12;
        private const int PB_PIN_START = 5;
        private GpioPin[] pushButtons = new GpioPin[NUM_OF_SOUNDS];
        private bool[] downStates = new bool[NUM_OF_SOUNDS];
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
