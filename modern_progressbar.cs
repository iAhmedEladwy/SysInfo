using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CNSysInfo
{
    /// <summary>
    /// Modern flat-design progress bar with smooth animations and customizable colors
    /// </summary>
    public class ModernProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;
        private Color _progressColor = Color.FromArgb(0, 120, 215);
        private Color _backgroundColor = Color.FromArgb(240, 240, 240);
        private string _text = "";
        private bool _showPercentage = true;

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(200, 24);
            Font = new Font("Segoe UI", 9F);
        }

        public int Value
        {
            get => _value;
            set
            {
                if (value < 0) value = 0;
                if (value > _maximum) value = _maximum;
                _value = value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value < 1) value = 1;
                _maximum = value;
                if (_value > _maximum) _value = _maximum;
                Invalidate();
            }
        }

        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        public Color ProgressBackColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                Invalidate();
            }
        }

        public override string Text
        {
            get => _text;
            set
            {
                _text = value;
                Invalidate();
            }
        }

        public bool ShowPercentage
        {
            get => _showPercentage;
            set
            {
                _showPercentage = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw background
            using (var bgBrush = new SolidBrush(_backgroundColor))
            {
                FillRoundedRectangle(g, bgBrush, 0, 0, Width, Height, 4);
            }

            // Calculate progress width
            float percent = _maximum > 0 ? (float)_value / _maximum : 0;
            int progressWidth = (int)(Width * percent);

            if (progressWidth > 0)
            {
                // Draw progress with gradient
                using (var progressBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, progressWidth, Height),
                    _progressColor,
                    ControlPaint.Light(_progressColor, 0.1f),
                    LinearGradientMode.Vertical))
                {
                    FillRoundedRectangle(g, progressBrush, 0, 0, progressWidth, Height, 4);
                }
            }

            // Draw text
            string displayText = _text;
            if (_showPercentage && string.IsNullOrEmpty(_text))
            {
                displayText = $"{(int)(percent * 100)}%";
            }

            if (!string.IsNullOrEmpty(displayText))
            {
                using (var textBrush = new SolidBrush(ForeColor))
                {
                    var textSize = g.MeasureString(displayText, Font);
                    float x = (Width - textSize.Width) / 2;
                    float y = (Height - textSize.Height) / 2;
                    g.DrawString(displayText, Font, textBrush, x, y);
                }
            }
        }

        private void FillRoundedRectangle(Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using (var path = GetRoundedRectangle(x, y, width, height, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private GraphicsPath GetRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }
}
