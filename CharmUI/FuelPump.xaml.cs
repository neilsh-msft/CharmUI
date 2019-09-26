using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.Custom;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CharmUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FuelPump : Page
    {
        double FuelFilled = 0;
        double FuelPrice = 0;
        DispatcherTimer DisplayDriver_Timer1 = new DispatcherTimer();
        double ScrollPos = 0;
        uint PosZeroDelay = 10;
        enum PumpStates { StateInitial = 0, StateFuelSelected, StatePumpStarted, StatePumpStopped };
        PumpStates PumpState = PumpStates.StateInitial;
        enum FuelTypes { FuelNotSelected = 0, FuelPremium, FuelRegular, FuelDiesel };
        FuelTypes FuelType = FuelTypes.FuelNotSelected;
        string StrAdBanner = "While you are waiting for your car to fill, just ignore the sign instructing you to never leave the pump unattended, and head into the store to grab a cup of coffee...              ";
        string StrStartFueling = "Start Fueling";
        string StrSelectFuel = "Select Fuel Type";
        string StrPauseStop = "Stop/Pause Pump";
        string StrResumeFueling = "Resume Pump";
        struct sUri {
            private readonly string display;
            private readonly string uri;
            private readonly int width;
            private readonly int height;

            public sUri(string display, int width, int height, string uri)
            {
                this.display = display;
                this.width = width;
                this.height = height;
                this.uri = uri;
            }

            public string Display { get { return display; } }
            public int Width { get { return width; } }
            public int Height { get { return height; } }
            public string Uri { get { return uri; } }
        }

        static readonly sUri[] MediaList =
            new[] {
             new sUri ("H264     1920x1080 @25FPS", 1920, 1080, "ms-appx:///Media/video-h264.mkv"),
             new sUri ("H264     640x480 @25FPS", 640, 480, "ms-appx:///Media/video-h264-480p.mkv"),
             new sUri ("H265     640x480 @25FPS", 640, 480, "ms-appx:///Media/video-hevc-480p.mkv"),
             new sUri ("H265     1920x1080 @25FPS", 1920, 1080, "ms-appx:///Media/video-hevc-h265.mkv"),
             new sUri ("H264 test bars 640x480 @25FPS", 640, 480, "ms-appx:///Media/video-h264-pattern.mkv"),
             new sUri ("H265 test bars 640x480 @25FPS", 640, 480, "ms-appx:///Media/video-hevc-pattern.mkv"),
             new sUri ("VP8  test bars 640x480 @25FPS", 640, 480, "ms-appx:///Media/video-vp8-pattern.mkv"),
             new sUri ("VP9  test bars 640x480 @25FPS", 640, 480, "ms-appx:///Media/video-vp9-pattern.mkv"),
             new sUri ("VP8      1920x1080 @25FPS", 1920, 1080, "ms-appx:///Media/video-vp8-webm.mkv"),
             new sUri ("VP9      1920x1080 @25FPS", 1920, 1080, "ms-appx:///Media/video-vp9.mkv"),
             new sUri ("H264 big_buck_bunny_480p_AAC_h264", 640, 480, "ms-appx:///Media/big_buck_bunny_480p_AAC_h264.mkv"),
             new sUri ("H265 big_buck_bunny_480p_AAC_h265", 640, 480, "ms-appx:///Media/big_buck_bunny_480p_AAC_h265.mkv"),
            };

        struct sVid {
            private readonly string display;
            private readonly int col;
            private readonly int row;

            public sVid(string display, int col, int row)
            {
                this.display = display;
                this.col = col;
                this.row = row;
            }

            public string Display { get { return display; } }
            public int Col { get { return col; } }
            public int Row { get { return row; } }
        }

        static readonly sVid[] VidFrameSize = new[]
        {
            new sVid ("Native", 0, 0),
            new sVid ("320x200 (CGA)", 320, 200),
            new sVid ("640x480 (VGA)", 640, 480),
            new sVid ("800x600", 800, 600),
            new sVid ("1280x720 (HD)", 1280, 720),
            new sVid ("1920x1080 (FHD)", 1920, 1080),
        };

        public FuelPump()
        {
            this.InitializeComponent();

            ResetPump();
            DisplayDriver_Timer1.Interval = new TimeSpan(0, 0, 0, 0, 100);
            DisplayDriver_Timer1.Tick += DisplayDriver_TimerTick;

            foreach (var l in MediaList)
                VidList.Items.Add(l.Display);

            VidList.SelectedIndex = 1;

            foreach (var s in VidFrameSize)
                VidSize.Items.Add(s.Display);
            //            VidSize.SelectedIndex = 0;
            VidSize.SelectedIndex = 2;
        }

        void DisplayDriver_TimerTick(object sender, object /*EventArgs*/ e)
        {
            try
            {
                if (PumpState == PumpStates.StatePumpStarted)
                {
                    if (FuelFilled < 99.92)
                        FuelFilled += 0.073;
                    double t = FuelFilled * FuelPrice;
                    SaleString.Text = $"{t:F2}";
                    GallonsString.Text = $"{FuelFilled:F3}";
                }

                ScrollContent.ChangeView(ScrollPos,null,null);
                if (PosZeroDelay > 0)
                {
                    PosZeroDelay--;
                    return;
                }
                if (ScrollContent.HorizontalOffset < (ScrollPos - 100))
                {
                    TxtBanner.Text = StrAdBanner;
                    ScrollPos = 0;
                    PosZeroDelay = 15;
                }
                else
                    ScrollPos += 10;
            }
            catch { }
        }

        private void BtnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void BtnFuelPremium_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelPremium);
        }

        private void BtnFuelRegular_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelRegular);
        }

        private void BtnFuelDiesel_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelDiesel);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPump();
        }

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            switch (PumpState)
            {
                case PumpStates.StateInitial:
                    break;

                case PumpStates.StateFuelSelected:
                    TxtBanner.Text = StrAdBanner;
                    ScrollContent.ChangeView(0, null, null);
                    DisplayDriver_Timer1.Start();
                    BtnStartStop.Content = StrPauseStop;

                    PumpState = PumpStates.StatePumpStarted;
                    break;

                case PumpStates.StatePumpStarted:

                    BtnStartStop.Content = StrResumeFueling;
                    PumpState = PumpStates.StatePumpStopped;
                    break;

                case PumpStates.StatePumpStopped:

                    BtnStartStop.Content = StrPauseStop;
                    PumpState = PumpStates.StatePumpStarted;
                    break;
            }
        }

        private void FuelSelection(FuelTypes ft)
        {
            if (FuelType == ft)
                return;

            FuelType = ft;
            FuelPrice = 0;

            if (ft == FuelTypes.FuelPremium)
            {
                BtnFuelPremium.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0xff, 0x03, 0x03));
                FuelPrice = 3.599;
            }
            else
                BtnFuelPremium.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));

            if (ft == FuelTypes.FuelRegular)
            {
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0xff, 0x03, 0x03));
                FuelPrice = 3.299;
            }
            else
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));

            if (ft == FuelTypes.FuelDiesel)
            {
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x0e, 0xff, 0x03));
                FuelPrice = 2.899;
            }
            else
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 0x0e, 0xff, 0x03));

            PriceString.Text = $"{FuelPrice:F3}";

            if (ft != FuelTypes.FuelNotSelected)
            {
                PumpState = PumpStates.StateFuelSelected;
                TxtBanner.Text = StrStartFueling;
                BtnStartStop.Content = StrStartFueling;
                BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 228, 146, 73));
            }
        }

        private void ResetPump()
        {
            DisplayDriver_Timer1.Stop();

            FuelSelection(FuelTypes.FuelNotSelected);
            SaleString.Text = "00.00";
            GallonsString.Text = "00.000";
            TxtBanner.Text = StrSelectFuel;
            BtnStartStop.Content = StrSelectFuel;
            BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 228, 146, 73));

            FuelFilled = 0;

            PumpState = PumpStates.StateInitial;
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    try {
/*
                        Guid imxvpumft = new Guid("{ada9253b-628c-40ce-b2c1-19f489a0f3da}");
                        string selector = CustomDevice.GetDeviceSelector(imxvpumft);
                        var allMfts = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(selector);

                        int c = allMfts.Count;
                        foreach (var element in allMfts)
                        {
                            var id = element.Id;
                            var name = element.Name;

                            var device = await CustomDevice.FromIdAsync(id, DeviceAccessMode.ReadWrite, DeviceSharingMode.Shared);
                            var ioctl = await device.SendIOControlAsync(new IOControlCode(0x47, 0, IOControlAccessMode.Any, IOControlBufferingMethod.Buffered), null, null);
                        }
*/

//                  FileStream foo = File.Open("\\\\?\\ACPI#NXP0109#0#{ada9253b-628c-40ce-b2c1-19f489a0f3da}", FileMode.Open);
//                  foo.Close();
                    } catch (Exception ex)
                    {
                        MessageDialog error = new MessageDialog(ex.Message);
                        await error.ShowAsync();
                    }
                    if (VidList.SelectedIndex < 0)
                    {
                        toggleSwitch.IsOn = false;
                    }
                    else
                    {
                        if (VidSize.SelectedIndex > 0)
                        {
                            VidFrame.Height = VidFrameSize[VidSize.SelectedIndex].Row;
                            VidFrame.Width = VidFrameSize[VidSize.SelectedIndex].Col;
                        }
                        else
                        {
                            VidFrame.Height = MediaList[VidList.SelectedIndex].Height;
                            VidFrame.Width = MediaList[VidList.SelectedIndex].Width;
                        }
                        System.Uri source = new System.Uri(MediaList[ VidList.SelectedIndex ].Uri);
                        VidFrame.MediaFailed += VidFrame_MediaFailed;
                        VidFrame.Source = source;
                        //                        VidFrame.AddVideoEffect("mft.mft0", true, null);
                        VidFrame.AreTransportControlsEnabled = true;
// Changing playback rate doesn't do anything on arm64 for H264 or H265 media :(
//                        double rate = VidFrame.DefaultPlaybackRate;
//                        rate = rate / 10;
//                        VidFrame.PlaybackRate = rate;
                        VidFrame.Play();
                    }
                }
                else
                {
                    VidFrame.Stop();
                }
            }
        }

        private async void VidFrame_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageDialog error = new MessageDialog(e.ErrorMessage);
            await error.ShowAsync();
        }
    }
}
