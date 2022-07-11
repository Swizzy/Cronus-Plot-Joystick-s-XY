using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using SharpDX.DirectInput;

namespace CronusXYJoystickPlot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Guid _joyGuid;
        private bool isNewDevice = true;
        private string _controllerName;
        private int _trailCount = 100;

        public MainWindow()
        {
            InitializeComponent();
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwOnDoWork;
            bw.RunWorkerAsync();
            DeviceBox.SelectionChanged += DeviceBoxOnSelectionChanged;
            StickLeft.Prefix = "L";
            StickRight.Prefix = "R";
            StickRight.SetTextRight();
            try
            {
                LoadConfig();
            }
            catch
            {
                //Ignore errors
            }
            StickLeft.TrailLimit = _trailCount;
            StickRight.TrailLimit = _trailCount;
            StickLeft.Resources["MarkerColor"] = Application.Current.Resources["LSColor"];
            StickRight.Resources["MarkerColor"] = Application.Current.Resources["RSColor"];
        }

        private void DeviceBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _joyGuid = DeviceBox.SelectedValue as Guid? ?? Guid.Empty;
            isNewDevice = true;
        }

        private void BwOnDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var di = new DirectInput();
                _joyGuid = Guid.Empty;
                JoystickState state = new JoystickState();
                Joystick joy = null;
                while (true)
                {
                    try
                    {
                        if (di.IsDeviceAttached(_joyGuid) == false)
                        {
                            var devs = di.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AttachedOnly)
                                         .Where(d => d.Type != DeviceType.Mouse
                                                     &&
                                                     d.Type != DeviceType.Keyboard)
                                         .ToList();
                            if (devs.Count > 1)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    DeviceBox.Items.Clear();
                                    DeviceBox.DisplayMemberPath = "Name";
                                    DeviceBox.SelectedValuePath = "Value";
                                    foreach (DeviceInstance dev in devs)
                                    {
                                        DeviceBox.Items.Add(new { Name = $"{dev.InstanceName} {dev.Type}", Value = dev.InstanceGuid });
                                    }
                                    DeviceBox.SelectedIndex = 0;
                                    DeviceBox.Visibility = Visibility.Visible;
                                    _joyGuid = DeviceBox.SelectedValue as Guid? ?? Guid.Empty;
                                });
                                Thread.Sleep(1000);
                            }
                            else
                            {
                                Dispatcher.Invoke(() => { DeviceBox.Visibility = Visibility.Collapsed; });
                                foreach (var deviceInstance in devs)
                                {
                                    _joyGuid = deviceInstance.InstanceGuid;
                                    isNewDevice = true;
                                }
                            }
                        }

                        if (di.IsDeviceAttached(_joyGuid))
                        {
                            if (isNewDevice)
                            {
                                joy = new Joystick(di, _joyGuid);
                                joy.Acquire();
                                isNewDevice = false;
                            }
                        }
                        else
                        {
                            joy = null;
                            _joyGuid = Guid.Empty;
                        }

                        if (joy != null)
                        {
                            joy.Poll();
                            state = joy.GetCurrentState();
                        }
                    }
                    catch
                    {
                        _joyGuid = Guid.Empty;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (_joyGuid != Guid.Empty)
                        {
                            ControllerName = $"{joy.Information.InstanceName} ({joy.Information.Type})";
                            NameBlock.SetValue(Canvas.LeftProperty, 400 - (NameBlock.ActualWidth / 2));
                            SetVisibility(Visibility.Collapsed, Visibility.Visible);
                            bool changed = false;
                            var newValue = (state.X - short.MaxValue) / (short.MaxValue / 100);
                            StickLeft.PolarX = state.X - short.MaxValue;
                            if (LX != newValue)
                            {
                                changed = true;
                                LX = newValue;
                            }
                            newValue = (state.Y - short.MaxValue) / (short.MaxValue / 100);
                            StickLeft.PolarY = state.Y - short.MaxValue;
                            if (LY != newValue)
                            {
                                changed = true;
                                LY = newValue;
                            }
                            if (changed)
                            {
                                StickLeft.AddTrail();
                            }
                            StickLeft.RefreshAngleRadius();

                            int rx, ry;
                            if (TreatAsPSController)
                            {
                                rx = state.Z;
                                ry = state.RotationZ;
                            }
                            else
                            {
                                rx = state.RotationX;
                                ry = state.RotationY;
                            }
                            changed = false;
                            newValue = (rx - short.MaxValue) / (short.MaxValue / 100);
                            StickRight.PolarX = rx - short.MaxValue;
                            if (RX != newValue)
                            {
                                changed = true;
                                RX = newValue;
                            }
                            newValue = (ry - short.MaxValue) / (short.MaxValue / 100);
                            StickRight.PolarY = ry - short.MaxValue;
                            if (RY != newValue)
                            {
                                changed = true;
                                RY = newValue;
                            }
                            if (changed)
                            {
                                StickRight.AddTrail();
                            }
                            StickRight.RefreshAngleRadius();
                        }
                        else
                        {
                            SetVisibility(Visibility.Visible, Visibility.Collapsed);
                        }
                    });
                    Thread.Sleep(10);
                }
            }
            catch
            {
                SetVisibility(Visibility.Visible, Visibility.Collapsed);
            }
        }

        public int RX
        {
            get => StickRight.X;
            set
            {
                StickRight.X = value;
                OnPropertyChanged();
            }
        }
        public int RY
        {
            get => StickRight.Y;
            set
            {
                StickRight.Y = value;
                OnPropertyChanged();
            }
        }

        public int LX
        {
            get => StickLeft.X;
            set
            {
                StickLeft.X = value;
                OnPropertyChanged();
            }
        }
        public int LY
        {
            get => StickLeft.Y;
            set
            {
                StickLeft.Y = value;
                OnPropertyChanged();
            }
        }

        public string ControllerName
        {
            get { return _controllerName; }
            set
            {
                _controllerName = value;
                OnPropertyChanged();
            }
        }

        public bool TreatAsPSController { get; set; }

        private void SetVisibility(Visibility noController, Visibility others)
        {
            try
            {
                NoControllerText.Visibility = noController;
                StickLeft.InputDisplay.Visibility = others;
                StickRight.InputDisplay.Visibility = others;
            }
            catch
            {
                //Ignore
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TreatAsPSController_Checked(object sender, RoutedEventArgs e)
        {
            TreatAsPSController = TreatAsPSControllerBox.IsChecked == true;
        }

        private void SetBrushColor(string name, string color)
        {
            try
            {
                Application.Current.Resources[name] = new BrushConverter().ConvertFromString(color);
            }
            catch 
            {
                // Ignore
            }
        }

        private void LoadConfig()
        {
            if (File.Exists("config.xml"))
            {
                var xml = XElement.Parse(File.ReadAllText("config.xml"));
                foreach (XElement element in xml.Elements())
                {
                    var name = element.Name.ToString();
                    if (name.Equals("Trails"))
                    {
                        if (int.TryParse(element.Value, NumberStyles.Any, null, out int trails) && trails >= 0)
                        {
                            _trailCount = trails;
                        }
                    }
                    else if (name.Equals("Background", StringComparison.CurrentCultureIgnoreCase))
                    {
                        SetBrushColor("BackgroundColor", element.Value);
                    }
                    else if (name.Equals("RS", StringComparison.CurrentCultureIgnoreCase))
                    {
                        SetBrushColor("RSColor", element.Value);
                    }
                    else if (name.Equals("LS", StringComparison.CurrentCultureIgnoreCase))
                    {
                        SetBrushColor("LSColor", element.Value);
                    }
                    else if (name.Equals("Outline", StringComparison.CurrentCultureIgnoreCase))
                    {
                        SetBrushColor("OutlineColor", element.Value);
                    }
                    else if (name.Equals("HideScaleMarkers"))
                    {
                        if ("true".Equals(element.Value, StringComparison.CurrentCultureIgnoreCase))
                        {
                            StickLeft.ScaleMarkers.Visibility = Visibility.Collapsed;
                            StickRight.ScaleMarkers.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.NewSize.Width / sizeInfo.NewSize.Height != 1700D / 880D)
            {
                double percentWidthChange = Math.Abs(sizeInfo.NewSize.Width - sizeInfo.PreviousSize.Width) / sizeInfo.PreviousSize.Width;
                double percentHeightChange = Math.Abs(sizeInfo.NewSize.Height - sizeInfo.PreviousSize.Height) / sizeInfo.PreviousSize.Height;

                if (percentWidthChange > percentHeightChange)
                {
                    Height = sizeInfo.NewSize.Width / (1700D / 880D);
                }
                else
                {
                    Width = sizeInfo.NewSize.Height * (1700D / 880D);
                }
            }

            base.OnRenderSizeChanged(sizeInfo);
        }
    }
}

