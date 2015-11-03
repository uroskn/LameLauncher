using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace LameLauncher
{
    class ConsoleLogger
    {
        public static DebugScr debugscreen;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod(int frame)
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(frame);

            return sf.GetMethod().Name;
        }

        public static void LogData(string par1)
        {
            ConsoleLogger.LogData(par1, 2);
        }

        public static void LogData(string par1, string sender)
        {
            string str = "[" + sender + "] " + par1;
            if (ConsoleLogger.debugscreen != null)
            {
                ConsoleLogger.debugscreen.Invoke(ConsoleLogger.debugscreen.logdata, str);
            }
			Console.WriteLine(str);
        }

        public static void LogData(string par1, int frame)
        {
            ConsoleLogger.LogData(par1, GetCurrentMethod(frame));
        }
    }
}
