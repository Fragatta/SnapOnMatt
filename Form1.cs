using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapOnTop
{
	public partial class Form1 : Form
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		internal static extern IntPtr GetForegroundWindow ();
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		internal static extern bool PrintWindow (IntPtr hWnd, IntPtr hdcBlt, int nFlags);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowRect (IntPtr hWnd, out Rect lpRect);

		[DllImport("user32.dll")]
		public static extern int SendMessage (IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture ();

		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
			public int Left;        // x position of upper-left corner
			public int Top;         // y position of upper-left corner
			public int Right;       // x position of lower-right corner
			public int Bottom;      // y position of lower-right corner
		}

		public Form1 ()
		{
			InitializeComponent();
			drawLoop.Tick += DrawLoop_Tick;
			hook.KeyPressed += Hook_KeyPressed;
			Reset();
		}

		private void Reset()
		{
			targetWindow = IntPtr.Zero;
			drawLoop.Enabled = false;
			label1.Visible = true;
			panel1.Invalidate();

			if (hotKeyRef.HasValue)
			{
				hook.UnregisterHotKey(hotKeyRef.Value);
				hotKeyRef = null;
			}
			hotKeyRef = hook.RegisterHotKey(KeyboardHook.ModifierKeys.Control, Keys.L);
			if (windowDraw != null)
			{
				windowDraw.Close();
				windowDraw = null;
			}
		}
		private int? hotKeyRef;

		private void StartDrawing(Rectangle rect)
		{
			hook.UnregisterHotKey(hotKeyRef.Value);
			hotKeyRef = null;

			targetRectangle = rect;
			drawLoop.Enabled = true;
			bitmap = new Bitmap(targetRectangle.X + targetRectangle.Width, targetRectangle.Y + targetRectangle.Height, PixelFormat.Format32bppPArgb);
			ClientSize = new Size(targetRectangle.Width, targetRectangle.Height);
			label1.Visible = false;
		}
		
		protected override void OnFormClosing (FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			hook.Dispose();
			if (bitmap != null)
				bitmap.Dispose();
		}

		private void Hook_KeyPressed (object sender, KeyboardHook.KeyPressedEventArgs e)
		{
			if (drawLoop.Enabled)
			{
				Reset();
			}

			targetWindow = GetForegroundWindow();
			Rect rect;
			if (!GetWindowRect(targetWindow, out rect))
			{
				return;
			}

			var width = rect.Right - rect.Left;
			var height = rect.Bottom - rect.Top;
			var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
			using (var gfxBmp = Graphics.FromImage(bmp))
			{
				var bmpHdc = gfxBmp.GetHdc();
				try
				{
					PrintWindow(targetWindow, bmpHdc, 0);
					panel1.Invalidate();
				}
				finally
				{
					gfxBmp.ReleaseHdc(bmpHdc);
				}
			}

			if (windowDraw != null)
			{
				windowDraw.Close();
				windowDraw = null;
			}

			windowDraw = new WindowDraw(bmp, rect.Left, rect.Top);
			windowDraw.RectangleDrawn += WindowDraw_RectangleDrawn;
			windowDraw.FormClosed += WindowDraw_FormClosed;
			windowDraw.ShowDialog();
		}

		private void WindowDraw_FormClosed (object sender, FormClosedEventArgs e)
		{
			windowDraw = null;
		}

		private void WindowDraw_RectangleDrawn (object sender, WindowDraw.RectangleDrawnEventArgs e)
		{
			((WindowDraw)sender).Close();
			StartDrawing(e.Rectangle);
		}
		
		private void DrawLoop_Tick (object sender, EventArgs e)
		{
			using (var gfxBmp = Graphics.FromImage(bitmap))
			{
				var bmpHdc = gfxBmp.GetHdc();
				try
				{
					PrintWindow(targetWindow, bmpHdc, 0);
					panel1.Invalidate();
				}
				finally
				{
					gfxBmp.ReleaseHdc(bmpHdc);
				}
			}
		}

		private void panel1_Paint (object sender, PaintEventArgs e)
		{
			if (bitmap != null && drawLoop.Enabled)
			{
				e.Graphics.DrawImage(bitmap, windowRectangle, targetRectangle, GraphicsUnit.Pixel);
			}
			else
			{
				e.Graphics.Clear(SystemColors.Control);
			}
		}

		private void Form1_Resize (object sender, EventArgs e)
		{
			windowRectangle = ClientRectangle;
		}

		private void Form1_MouseDown (object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		private void Form1_KeyDown (object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (drawLoop.Enabled)
				{
					Reset();
				}
				else
				{
					Close();
				}
			}
			else if (e.KeyCode == Keys.L && e.Modifiers == Keys.Control)
			{
				ClientSize = new Size(targetRectangle.Width, targetRectangle.Height);
			}
		}

		private WindowDraw windowDraw;
		private KeyboardHook hook = new KeyboardHook();
		private Timer drawLoop = new Timer() { Interval = 30 };
		private Bitmap bitmap;
		private IntPtr targetWindow;
		private Rectangle targetRectangle;
		private Rectangle windowRectangle;
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;
	}
}
