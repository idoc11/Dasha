using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SEO_LoadSimulator
{
    /// <summary>
    /// Простой график: столбцы или линия, пунктирная линия норматива.
    /// </summary>
    public class ChartBox : Control
    {
        double[] _values = new double[0];
        string[] _labels = new string[0];
        bool _bars;
        double _limit = double.NaN;
        int _labelStep = 1;
        string _title = "";

        public ChartBox()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        public void SetData(
            string title,
            double[] values,
            string[] labels,
            bool bars,
            double limit,
            int labelStep
        )
        {
            _title = title;
            _values = values;
            _labels = labels;
            _bars = bars;
            _limit = limit;
            _labelStep = Math.Max(1, labelStep);
            Invalidate();
        }

        void DrawCentered(Graphics g, string text)
        {
            var size = g.MeasureString(text, Font);
            g.DrawString(
                text,
                Font,
                Brushes.DimGray,
                (Width - size.Width) / 2,
                (Height - size.Height) / 2
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            using (var bold = new Font(Font, FontStyle.Bold))
            {
                var ts = g.MeasureString(_title, bold);
                g.DrawString(
                    _title,
                    bold,
                    Brushes.Black,
                    (Width - ts.Width) / 2,
                    4
                );
            }
            if (_values.Length == 0)
            {
                DrawCentered(g, "Нет данных");
                return;
            }

            var plot = new RectangleF(
                54,
                30,
                Math.Max(10, Width - 54 - 14),
                Math.Max(10, Height - 30 - 34)
            );
            double max = 0;
            foreach (var v in _values)
                if (v > max)
                    max = v;
            if (!double.IsNaN(_limit) && _limit > max)
                max = _limit;
            max *= 1.1;
            if (max <= 0)
                max = 1;

            using (var grid = new Pen(Color.Gainsboro))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Bottom - plot.Height * i / 4f;
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                    string s = (max * i / 4).ToString(
                        max < 10 ? "F1" : "F0"
                    );
                    var sz = g.MeasureString(s, Font);
                    g.DrawString(
                        s,
                        Font,
                        Brushes.Gray,
                        plot.Left - sz.Width - 2,
                        y - sz.Height / 2
                    );
                }
            }
            g.DrawRectangle(
                Pens.Gray,
                plot.Left,
                plot.Top,
                plot.Width,
                plot.Height
            );

            int n = _values.Length;
            float step = plot.Width / n;
            if (_bars)
            {
                float bw = step * 0.7f;
                for (int i = 0; i < n; i++)
                {
                    float h = (float)(_values[i] / max) * plot.Height;
                    g.FillRectangle(
                        Brushes.SteelBlue,
                        plot.Left + i * step + (step - bw) / 2,
                        plot.Bottom - h,
                        bw,
                        h
                    );
                }
            }
            else if (n > 1)
            {
                var pts = new PointF[n];
                for (int i = 0; i < n; i++)
                    pts[i] = new PointF(
                        plot.Left + (i + 0.5f) * step,
                        plot.Bottom
                            - (float)(_values[i] / max) * plot.Height
                    );
                using (var pen = new Pen(Color.SteelBlue, 2f))
                    g.DrawLines(pen, pts);
            }

            // шаг подписей увеличивается, пока подписи не перестанут
            // накладываться друг на друга
            int every = _labelStep;
            if (_labels.Length > 0)
            {
                float lw = g.MeasureString(_labels[0], Font).Width + 6;
                while (every * step < lw)
                    every += _labelStep;
            }
            for (int i = 0; i < n && i < _labels.Length; i += every)
            {
                var sz = g.MeasureString(_labels[i], Font);
                g.DrawString(
                    _labels[i],
                    Font,
                    Brushes.Black,
                    plot.Left + (i + 0.5f) * step - sz.Width / 2,
                    plot.Bottom + 3
                );
            }

            if (!double.IsNaN(_limit))
            {
                float y =
                    plot.Bottom - (float)(_limit / max) * plot.Height;
                using (
                    var pen = new Pen(Color.Red, 1.5f)
                    {
                        DashStyle = DashStyle.Dash,
                    }
                )
                    g.DrawLine(pen, plot.Left, y, plot.Right, y);
                string s = string.Format("норматив {0:F0} мин", _limit);
                var sz = g.MeasureString(s, Font);
                // белая подложка, чтобы подпись читалась поверх
                // столбцов
                g.FillRectangle(
                    Brushes.White,
                    plot.Right - sz.Width,
                    y - sz.Height,
                    sz.Width,
                    sz.Height - 1
                );
                g.DrawString(
                    s,
                    Font,
                    Brushes.Red,
                    plot.Right - sz.Width,
                    y - sz.Height
                );
            }
        }
    }
}
