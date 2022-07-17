using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace CronusXYJoystickPlot
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class StickPlot : INotifyPropertyChanged
    {
        private int _x, _polarX;
        private int _y, _polarY;

        public StickPlot()
        {
            InitializeComponent();
        }

        public string Prefix { get; set; }
        public int TrailLimit { get; set; }

        public int X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(XOffset));
                OnPropertyChanged(nameof(XText));
            }
        }
        public double XOffset => GetOffset(X);
        public string XText => $"{Prefix}X = {X}";

        public int PolarX
        {
            get => _polarX;
            set
            {
                _polarX = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PolarXText));
            }
        }
        public string PolarXText => $"Polar {Prefix}X = {PolarX}";

        public int Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YOffset));
                OnPropertyChanged(nameof(YText));
            }
        }
        public double YOffset => GetOffset(Y);
        public string YText => $"{Prefix}Y = {Y}";

        public int PolarY
        {
            get => _polarY;
            set
            {
                _polarY = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PolarYText));
            }
        }
        public string PolarYText => $"Polar {Prefix}Y = {PolarY}";

        public int PolarAngle { get; private set; }
        public int PolarRadius { get; private set; }
        public string PolarAngleText => $"Polar Angle: {PolarAngle}";
        public string PolarRadiusText => $"Polar Radius: {PolarRadius}";

        public void RefreshAngleRadius()
        {
            double radius = Math.Sqrt(Math.Pow(Math.Abs(PolarX), 2) + Math.Pow(Math.Abs(PolarY), 2));
            double angle = 0;
            if (PolarX != 0)
            {
                angle = 180 * Math.Acos(PolarX / radius) / Math.PI;
            }
            else if (PolarY != 0)
            {
                angle = 180 * Math.Asin(PolarY / radius) / Math.PI;
            }
            if (PolarY > 0)
            {
                angle = 360 - Math.Abs(angle);
            }
            PolarRadius = (int)Math.Round(radius);
            OnPropertyChanged(nameof(PolarRadius));
            OnPropertyChanged(nameof(PolarRadiusText));
            PolarAngle = (int)Math.Round(angle);
            if (PolarAngle == 360)
            {
                PolarAngle = 0;
            }
            OnPropertyChanged(nameof(PolarAngle));
            OnPropertyChanged(nameof(PolarAngleText));
        }

        public void AddTrail()
        {
            RefreshAngleRadius();
            if (TrailLimit == 0)
            {
                return;
            }
            if (TrailCanvas.Children.Count > TrailLimit)
            {
                TrailCanvas.Children.RemoveAt(0);
            }
            Rectangle trailPointer = new Rectangle
                                     {
                                         Width = 2,
                                         Height = 2
                                     };
            trailPointer.SetResourceReference(Shape.FillProperty, "MarkerColor");
            trailPointer.SetValue(Canvas.LeftProperty, XOffset + 5);
            trailPointer.SetValue(Canvas.TopProperty, YOffset + 5);
            TrailCanvas.Children.Add(trailPointer);
        }

        public void SetTextRight()
        {
            TextLeft.Visibility = Visibility.Collapsed;
            TextRight.Visibility = Visibility.Visible;
        }

        private double GetOffset(int axis)
        {
            return axis * 3.50 + 430;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
