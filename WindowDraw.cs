using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnapOnTop
{
	public partial class WindowDraw : Form
	{
		public WindowDraw (Bitmap window, int x, int y)
		{
			InitializeComponent();

			_window = window;
			Left = x;
			Top = y;
			Width = window.Width;
			Height = window.Height;
		}

		protected override void OnFormClosing (FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			_window.Dispose();
		}

		private void panel1_Paint (object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage(_window, 0, 0);
			if (_rectangle != null && _rectangle != Rectangle.Empty)
			{
				e.Graphics.DrawRectangle(new Pen(Brushes.Black, 2), _rectangle);
			}
		}

		private void panel1_MouseDown (object sender, MouseEventArgs e)
		{
			_point = new Point(e.X, e.Y);
		}

		private void panel1_MouseUp (object sender, MouseEventArgs e)
		{
			if (RectangleDrawn != null)
			{
				RectangleDrawn(this, new RectangleDrawnEventArgs(_rectangle));
			}
			Close();
		}

		private void panel1_MouseMove (object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				var x1 = Math.Min(_point.X, e.X);
				var y1 = Math.Min(_point.Y, e.Y);
				var width = Math.Abs(_point.X - e.X);
				var height = Math.Abs(_point.Y - e.Y);
				_rectangle = new Rectangle(x1, y1, width, height);
				panel1.Invalidate();
			}
		}

		private void WindowDraw_KeyDown (object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		public event EventHandler<RectangleDrawnEventArgs> RectangleDrawn;

		public class RectangleDrawnEventArgs : EventArgs
		{
			internal RectangleDrawnEventArgs (Rectangle rect)
			{
				Rectangle = rect;
			}

			public Rectangle Rectangle { get; private set; }
		}

		private Point _point;
		private Bitmap _window;
		private Rectangle _rectangle;
	}
}
