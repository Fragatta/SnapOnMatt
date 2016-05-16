using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapOnTop
{
	public sealed class KeyboardHook : IDisposable
	{
		// Registers a hot key with Windows.
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey (IntPtr hWnd, int id, uint fsModifiers, uint vk);
		// Unregisters the hot key with Windows.
		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey (IntPtr hWnd, int id);

		public KeyboardHook ()
		{
			// register the event of the inner native window.
			_window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
			{
				if (KeyPressed != null)
					KeyPressed(this, args);
			};
		}

		public void UnregisterHotKey (int i)
		{
			if (!_ids.Contains(i))
			{
				//throw new InvalidOperationException("That hotkey id isn't registered, you doofus!");
				return;
			}
			UnregisterHotKey(_window.Handle, i);
			_ids.Remove(i);
		}

		public int RegisterHotKey (ModifierKeys modifier, Keys key)
		{
			// increment the counter.
			var id = _ids.Any() ? _ids.Max() + 1 : 0;
			_ids.Add(id);

			// register the hot key.
			if (!RegisterHotKey(_window.Handle, id, (uint)modifier, (uint)key))
				throw new InvalidOperationException("Couldn’t register the hot key.");
			return id;
		}

		public event EventHandler<KeyPressedEventArgs> KeyPressed;

		public void Dispose ()
		{
			// unregister all the registered hot keys.
			foreach (var id in _ids)
			{
				UnregisterHotKey(_window.Handle, id);
			}

			// dispose the inner native window.
			_window.Dispose();
		}
		
		private Window _window = new Window();
		private List<int> _ids = new List<int>();


		public class KeyPressedEventArgs : EventArgs
		{
			private ModifierKeys _modifier;
			private Keys _key;

			internal KeyPressedEventArgs (ModifierKeys modifier, Keys key)
			{
				_modifier = modifier;
				_key = key;
			}

			public ModifierKeys Modifier
			{
				get { return _modifier; }
			}

			public Keys Key
			{
				get { return _key; }
			}
		}

		[Flags]
		public enum ModifierKeys : uint
		{
			Alt = 1,
			Control = 2,
			Shift = 4,
			Win = 8
		}

		private class Window : NativeWindow, IDisposable
		{
			private static int WM_HOTKEY = 0x0312;

			public Window ()
			{
				CreateHandle(new CreateParams());
			}

			protected override void WndProc (ref Message m)
			{
				base.WndProc(ref m);

				if (m.Msg == WM_HOTKEY)
				{
					Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
					var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

					// invoke the event to notify the parent.
					if (KeyPressed != null)
						KeyPressed(this, new KeyPressedEventArgs(modifier, key));
				}
			}

			public event EventHandler<KeyPressedEventArgs> KeyPressed;

			public void Dispose ()
			{
				DestroyHandle();
			}
		}
	}
}
