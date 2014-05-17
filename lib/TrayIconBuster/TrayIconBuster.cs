using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;		// DllImport
using System.Text;

namespace TrayIconBuster {
	class TrayIconBuster {
		private const uint TB_BUTTONCOUNT=			0x0418;	// WM_USER+24
		private const uint TB_GETBUTTON=			0x0417;	// WM_USER+23
		private const uint TB_DELETEBUTTON=			0x0416;	// WM_USER+22

		private static object key=new object(); // concurrency protection

		// for debug purposes only
		private static void log(string s) {
			Console.WriteLine(DateTime.Now.ToLongTimeString()+" "+s);
		}

		/// <summary>
		/// The actual trayIconBuster
		/// </summary>
		/// <returns>The number of tray icons removed.</returns>
		public static uint RemovePhantomIcons() {
			uint removedCount=0;
			lock(key) {			// prevent concurrency problems
				IntPtr hWnd=IntPtr.Zero;
				FindNestedWindow(ref hWnd, "Shell_TrayWnd");
				FindNestedWindow(ref hWnd, "TrayNotifyWnd");
				FindNestedWindow(ref hWnd, "SysPager");
				FindNestedWindow(ref hWnd, "ToolbarWindow32");
				// create an object so we can exchange data with other process
				using(LP_Process process=new LP_Process(hWnd)) {
					ToolBarButton tbb=new ToolBarButton();
					IntPtr remoteButtonPtr=process.Allocate(tbb);
					TrayData td=new TrayData();
					process.Allocate(td);
					uint itemCount=(uint)SendMessage(hWnd, TB_BUTTONCOUNT,
						IntPtr.Zero, IntPtr.Zero);
					log("There are "+itemCount+" tray icons (some of them hidden)");
					bool foundSomeExe=false;
					// for safety reasons we perform two passes:
					// pass1 = search for my own NotifyIcon
					// pass2 = search phantom icons and remove them
					//         pass2 doesnt happen if pass1 fails
					for(int pass=1; pass<=2; pass++) {
						for(uint item=0; item<itemCount; item++) {
							// index changes when previous items got removed !
							uint item2=item-removedCount;
							uint SOK=(uint)SendMessage(hWnd, TB_GETBUTTON,
								new IntPtr(item2), remoteButtonPtr);
							if(SOK!=1) throw new ApplicationException("TB_GETBUTTON failed");
							process.Read(tbb, remoteButtonPtr);
							process.Read(td, tbb.dwData);
							if(td.hWnd==IntPtr.Zero) throw new ApplicationException("Invalid window handle");
							using(LP_Process proc=new LP_Process(td.hWnd)) {
								string filename=proc.GetImageFileName();
								if(pass==1 && filename!=null) {
									filename=filename.ToLower();
									if(filename.EndsWith(".exe")) {
										foundSomeExe=true;
										log("found real icon created by: "+filename);
										break;
									}
								}
								// a phantom icon has no imagefilename
								if(pass==2 && filename==null) {
									SOK=(uint)SendMessage(hWnd, TB_DELETEBUTTON,
										new IntPtr(item2), IntPtr.Zero);
									if(SOK!=1) throw new ApplicationException("TB_DELETEBUTTON failed");
									removedCount++;
								}
							}
						}
						// if I did not see myself, I will not run the second
						// pass, which would try and remove phantom icons
						if(!foundSomeExe) throw new ApplicationException(
						   "Failed to find any real icon");
					}
				}
				log(removedCount.ToString()+" icons removed");
			}
			return removedCount;
		}

		// Find a topmost or nested window with specified name
		private static void FindNestedWindow(ref IntPtr hWnd, string name) {
			if(hWnd==IntPtr.Zero) {
				hWnd=FindWindow(name, null);
			} else {
				hWnd=FindWindowEx(hWnd, IntPtr.Zero, name, null);
			}
			if(hWnd==IntPtr.Zero) throw new ApplicationException("Failed to locate window "+name);
		}

		[DllImport("user32.dll", EntryPoint="SendMessageA",
			CallingConvention=CallingConvention.StdCall)]
		public static extern IntPtr SendMessage(IntPtr Hdc, uint Msg_Const,
			IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", EntryPoint="FindWindowA",
			 CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi)]
		public static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

		[DllImport("user32.dll", EntryPoint="FindWindowExA",
			 CallingConvention=CallingConvention.StdCall, CharSet=CharSet.Ansi)]
		public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
			string lpszClass, string lpszWindow);
		
		/// <summary>
		/// ToolBarButton struct used for TB_GETBUTTON message.
		/// </summary>
		/// <remarks>We use a class so LP_Process.Read can fill this.</remarks>
		[StructLayout(LayoutKind.Sequential)]
		public class ToolBarButton {
			public uint iBitmap;
			public uint idCommand;
			public byte fsState;
			public byte fsStyle;
			private byte bReserved0;
			private byte bReserved1;
			public IntPtr dwData;	// points to tray data
			public uint iString;
		}

		/// <summary>
		/// TrayData struct used for extra info for ToolBarButton.
		/// </summary>
		/// <remarks>We use a class so LP_Process.Read can fill this.</remarks>
		[StructLayout(LayoutKind.Sequential)]
		public class TrayData {
			public IntPtr hWnd;
			public uint uID;
			public uint uCallbackMessage;
			private uint reserved0;
			private uint reserved1;
			public IntPtr hIcon;
		}
	}
}