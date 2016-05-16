using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapOnTop
{
	public partial class WindowDraw : Form
	{
		public WindowDraw (Bitmap window, int x, int y)
		{
			InitializeComponent();

			_window = window;
			CreateDarkWindow();

			Left = x;
			Top = y;
			Width = window.Width;
			Height = window.Height;
		}

		private void CreateDarkWindow()
		{
			var width = _window.Width;
			var height = _window.Height;
			var pixelFormat = _window.PixelFormat;
			var pixelCount = width * height;
			var rect = new Rectangle (0, 0, width, height);

			_darkWindow = new Bitmap (width, height, pixelFormat);
			//GetPixel / SetPixel will probably be fast enough given this is only called once, nevermind it's REALLY SLOW
			var sourceBitData = _window.LockBits (rect, ImageLockMode.ReadOnly, pixelFormat);
			var targetBitData = _darkWindow.LockBits (rect, ImageLockMode.WriteOnly, pixelFormat);
			var depth = Image.GetPixelFormatSize (pixelFormat);
			if (depth != 24 && depth != 32)
			{
				throw new ArgumentException ("Only 24 and 32 bpp images are supported.");
			}

			// Argb format seems to be in rgba format so don't skip the first byte, seems like this will come back to byte me (sorry)
			var step = depth / 8;
			var stride = sourceBitData.Stride;
			var ptrSource = sourceBitData.Scan0;
			var ptrTarget = targetBitData.Scan0;

			var bytes = sourceBitData.Stride * height;
			var rgbValues = new byte[bytes];
			Marshal.Copy (ptrSource, rgbValues, 0, bytes);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var i = (y * stride) + (x * step);
					rgbValues[i + 0] = (byte)Math.Min (255, rgbValues[i + 0] + 50);
					rgbValues[i + 1] = (byte)Math.Min (255, rgbValues[i + 1] + 50);
					rgbValues[i + 2] = (byte)Math.Min (255, rgbValues[i + 2] + 50);
				}
			}
			Marshal.Copy (rgbValues, 0, ptrTarget, bytes);
			_window.UnlockBits (sourceBitData);
			_darkWindow.UnlockBits (targetBitData);
		}

		protected override void OnFormClosing (FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			_window.Dispose();
		}

		private void panel1_Paint (object sender, PaintEventArgs e)
		{
			e.Graphics.DrawImage (_darkWindow, 0, 0);
			if (_rectangle != null && _rectangle != Rectangle.Empty)
			{
				e.Graphics.DrawImage (_window, _rectangle, _rectangle, GraphicsUnit.Pixel);
				e.Graphics.DrawRectangle(new Pen(Brushes.Black, 2), _rectangle);
			}
		}

		private void panel1_MouseDown (object sender, MouseEventArgs e)
		{
			_point = new Point (e.X, e.Y);
		}

		private void panel1_MouseUp (object sender, MouseEventArgs e)
		{
			if (_rectangle == null || _rectangle == Rectangle.Empty)
			{
				return;
			}

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
		private Bitmap _darkWindow;
		private Rectangle _rectangle;
	}
}
