using System;
using System.Threading;

using dotNSASM;

namespace Native.Csharp.App.NSASM
{
    public class Helper
    {
        public static void Init()
        {
            Util.Output = (value) => { };
            Util.Input = () => { return "null"; };
            Util.FileInput = (path) => { return ""; };
            Util.BinaryInput = (path) => { return null; };
            Util.BinaryOutput = (path, bytes) => { };
        }

        public static string Execute(string code, long group)
        {
            if (code.Length > 512)
                return "嗝";

            string result = "";
            Util.Output = (value) => { result += value; };
            code = code.Replace(";;", "\n");
            var core = new dotNSASM.NSASM(512, 512, 64, Util.GetSegments(code));
            core.SetGroupID(group);
            Thread thread = new Thread(() => core.Run());
            int start = Environment.TickCount;
            thread.Start();
            while (thread.IsAlive)
            {
                if (start - Environment.TickCount > 1000)
                {
                    thread.Abort();
                    result = "已超时 | Time Exceed";
                }
            }
            Util.Output = (value) => { };

            return result;
        }
    }
}
