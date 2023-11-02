using System.Reflection;
using System.Runtime.InteropServices;

namespace External
{
    public class Utils
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public Int32 dwTimeout;
        }

        public const UInt32 FLASHW_ALL = 3;

        public static void FlashWindow(IntPtr hWnd)
        {
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = FLASHW_ALL;
            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            FlashWindowEx(ref fInfo);
        }
        public static void Config(Type type)
        {
            string path = "config.txt";
            using (StreamReader reader = new StreamReader(path))
            {
                Console.WriteLine("Loaded config with: ");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] var = line.Split('=');
                    FieldInfo field = type.GetField(var[0], BindingFlags.Static | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        if (int.TryParse(var[1], out int value))
                            field.SetValue(null, value);
                        else field.SetValue(null, var[1]);
                        Console.WriteLine(line);
                    }
                }
            }
        }
        public static string Log(string message)
        {
            string path = "log.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
                writer.WriteLine(message);
            return message;
        }
    }
}