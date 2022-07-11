﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpDX.DirectInput;

namespace CronusXYJoystickPlot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Guid _joyGuid;
        private int _rx;
        private int _ry;
        private int _lx;
        private int _ly;
        private bool isNewDevice = true;
        private string _controllerName;

        public MainWindow()
        {
            InitializeComponent();
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwOnDoWork;
            bw.RunWorkerAsync();
            DeviceBox.SelectionChanged += DeviceBoxOnSelectionChanged;
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
                            if (LX != newValue)
                            {
                                changed = true;
                                LX = newValue;
                            }
                            newValue = (state.Y - short.MaxValue) / (short.MaxValue / 100);
                            if (LY != newValue)
                            {
                                changed = true;
                                LY = newValue;
                            }
                            if (changed)
                            {
                                if (LSTrail.Children.Count > 100)
                                {
                                    LSTrail.Children.RemoveAt(0);
                                }
                                LSTrail.Children.Add(MakeTrail(LXOffset, LYOffset, "LSColor"));
                            }

                            changed = false;
                            newValue = (state.RotationX - short.MaxValue) / (short.MaxValue / 100);
                            if (RX != newValue)
                            {
                                changed = true;
                                RX = newValue;
                            }
                            newValue = (state.RotationY - short.MaxValue) / (short.MaxValue / 100);
                            if (RY != newValue)
                            {
                                changed = true;
                                RY = newValue;
                            }
                            if (changed)
                            {
                                if (RSTrail.Children.Count > 100)
                                {
                                    RSTrail.Children.RemoveAt(0);
                                }
                                RSTrail.Children.Add(MakeTrail(RXOffset, RYOffset, "RSColor"));
                            }
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

        private Rectangle MakeTrail(double x, double y, string color)
        {
            var trailPointer = new Rectangle();
            trailPointer.SetValue(Canvas.LeftProperty, x + 5);
            trailPointer.SetValue(Canvas.TopProperty, y + 5);
            trailPointer.Fill = (Brush)Resources[color];
            trailPointer.Width = 2;
            trailPointer.Height = 2;
            return trailPointer;
        }

        public int RX
        {
            get => _rx;
            set
            {
                _rx = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RXOffset));
                OnPropertyChanged(nameof(RXText));
            }
        }
        public double RXOffset => GetOffset(RX);
        public string RXText => string.Format("RX = {0}", RX);

        public int RY
        {
            get => _ry;
            set
            {
                _ry = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RYOffset));
                OnPropertyChanged(nameof(RYText));
            }
        }
        public double RYOffset => GetOffset(RY);
        public string RYText => string.Format("RY = {0}", RY);

        public int LX
        {
            get => _lx;
            set
            {
                _lx = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LXOffset));
                OnPropertyChanged(nameof(LXText));
            }
        }

        public double LXOffset => GetOffset(LX);
        public string LXText => string.Format("LX = {0}", LX);

        public int LY
        {
            get => _ly;
            set
            {
                _ly = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LYOffset));
                OnPropertyChanged(nameof(LYText));
            }
        }

        public double LYOffset => GetOffset(LY);
        public string LYText => string.Format("LY = {0}", LY);

        public string ControllerName
        {
            get { return _controllerName; }
            set
            {
                _controllerName = value;
                OnPropertyChanged();
            }
        }

        private double GetOffset(int axis)
        {
            return (axis * 3.50) + 390;
        }

        private void SetVisibility(Visibility noController, Visibility others)
        {
            try
            {
                NoControllerText.Visibility = noController;
                InputDisplay.Visibility = others;
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
    }
}