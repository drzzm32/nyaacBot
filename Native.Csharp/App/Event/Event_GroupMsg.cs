using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using Native.Csharp.Sdk.Cqp.EventArgs;
using Native.Csharp.Sdk.Cqp.Interface;

namespace Native.Csharp.App.Event
{
    public class Event_GroupMsg : IReceiveGroupMessage
    {
        private readonly string PING_CMD = "$bot.ping";
        private readonly string PONG_CMD = "$bot.pong";
        private readonly string PA_CMD = "$bot.pa";

        private readonly string LCSC_CMD = "$bot.lcsc";

        private readonly string NSASM_CMD = "$bot.nsasm";

        public struct Repeat
        {
            public bool RepeatEnb;
            public float RepeatCtl;
            public static Repeat Def() { return new Repeat() { RepeatEnb = false, RepeatCtl = 0.25F }; }
        };
        private static Dictionary<long, Repeat> RepeatCtls = new Dictionary<long, Repeat>();
        private static readonly object _lock = new object();

        public static void SetRepeatEnb(long gid, bool val)
        {
            Monitor.Enter(_lock);
            if (!RepeatCtls.ContainsKey(gid))
                RepeatCtls[gid] = Repeat.Def();
            var ctl = RepeatCtls[gid];
            ctl.RepeatEnb = val;
            RepeatCtls[gid] = ctl;
            Monitor.Exit(_lock);
        }

        public static void SetRepeatLim(long gid, float val)
        {
            Monitor.Enter(_lock);
            if (!RepeatCtls.ContainsKey(gid))
                RepeatCtls[gid] = Repeat.Def();
            var ctl = RepeatCtls[gid];
            ctl.RepeatCtl = val > 1 ? 1 : (val < 0 ? 0 : val);
            RepeatCtls[gid] = ctl;
            Monitor.Exit(_lock);
        }

        private Random random = new Random();

        public void ReceiveGroupMessage(object sender, CqGroupMessageEventArgs e)
        {
            if (e.Message.ToLower() == PING_CMD)
            {
                Common.CqApi.SendGroupMessage(e.FromGroup, "PONG!");
            }
            else if (e.Message.ToLower() == PONG_CMD)
            {
                Common.CqApi.SendGroupMessage(e.FromGroup, "啪！");
            }
            else if (e.Message.ToLower() == PA_CMD)
            {
                Common.CqApi.SendGroupMessage(e.FromGroup, "糊糊！");
            }
            else if (e.Message.StartsWith(LCSC_CMD))
            {
                string arg = e.Message.Substring(LCSC_CMD.Length);
                arg = arg.Replace("\t", " ");
                while (arg.StartsWith(" "))
                    arg = arg.Substring(1);
                using (WebClient client = new WebClient())
                {
                    StreamReader reader = new StreamReader(client.OpenRead("https://so.szlcsc.com/global.html?k=" + arg));
                    string str = reader.ReadToEnd();
                    reader.Close();
                    if (!str.Contains("product-tbody-line"))
                    {
                        Common.CqApi.SendGroupMessage(e.FromGroup, "咩找到！");
                        return;
                    }
                    str = str.Substring(str.IndexOf("product-tbody-line"));
                    str = GetStr(str, "<tbody>", "</tbody>");

                    str = str.Substring(str.IndexOf("<div class=\"two-tit\">"));
                    string part = GetStr(str, "<a title=\"", "\"").Replace(" ", "");
                    str = str.Substring(str.IndexOf("</a>"));
                    string catalog = GetStr(str, "<a title=\"", "\"").Replace(" ", "");
                    str = str.Substring(str.IndexOf("</a>"));

                    str = str.Substring(str.IndexOf("<ul class=\"l02-zb\">"));
                    str = str.Substring(str.IndexOf("<span class=\""));
                    string id = GetStr(str, "</span>", "\n");
                    str = str.Substring(str.IndexOf("<span title=\""));
                    string pack = GetStr(str, "<span title=\"", "\"");

                    str = str.Substring(str.IndexOf("<a class=\"brand-name"));
                    string brand = GetStr(str, ">\n", "\n").Replace(" ", "");

                    str = str.Substring(str.IndexOf("<div class=\"lower\">"));
                    string desc = GetStr(str, "<div class=\"ellipsis\" title=\"", "\">");
                    if (desc == "") desc = "么有（";

                    str = str.Substring(str.IndexOf("<p class=\"ppbbz-p\">"));
                    string val = GetStr(str, "data-productPrice=\"", "\"");
                    string min = GetStr(str, "data-startPurchasedNumber=\"", "\"");

                    str = str.Substring(str.IndexOf("<ul class=\"pan\">"));
                    str = str.Substring(str.IndexOf("<li class=\"pan-list\">"));
                    string back = GetStr(str, "<li class=\"pan-list\">", "<span class=\"goldenrod\">").Replace("\n", "").Replace(" ", "");
                    back += GetStr(str, "<span class=\"goldenrod\">", "</span>").Replace("\n", "").Replace(" ", "");
                    back += GetStr(str, "</span>", "</li>").Replace("\n", "").Replace(" ", "");
                    str = str.Substring(str.IndexOf("</li>"));
                    str = str.Substring(str.IndexOf("<li class=\"pan-list\">"));
                    string has = GetStr(str, "<li class=\"pan-list\">", "<i").Replace("\n", "").Replace(" ", "");
                    if (has == "" || str.Contains("arrival-notice-btn"))
                        has = "么有货，等待到货";

                    string res = "搜索 " + arg + " 的结果如下！\n";
                    res += (catalog + "/" + part + "\n");
                    res += ("编号：" + id + "，封装：" + pack + "\n");
                    res += ("品牌：" + brand + "\n");
                    res += ("\n介绍：\n" + desc + "\n\n");
                    res += ("起订量：" + min + "+，单价：￥" + val + "\n");
                    res += ("规格：" + back + "，状态：" + has + "\n");
                    Common.CqApi.SendGroupMessage(e.FromGroup, res);
                }
            }
            else if (e.Message.StartsWith(NSASM_CMD))
            {
                string code = e.Message.Substring(NSASM_CMD.Length);
                string result = "";
                try
                {
                    result = NSASM.Helper.Execute(code, e.FromGroup);
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                }
                Common.CqApi.SendGroupMessage(e.FromGroup, result);
            }
            else
            {
                long id = e.FromGroup;
                if (!RepeatCtls.ContainsKey(id))
                    RepeatCtls[id] = Repeat.Def();

                if (RepeatCtls[id].RepeatEnb)
                {
                    float rand = (float)random.NextDouble();
                    if (rand < RepeatCtls[id].RepeatCtl)
                        Common.CqApi.SendGroupMessage(e.FromGroup, e.Message);
                }
            }
        }

        public string GetStr(string content, string start, string end)
        {
            var posStart = content.IndexOf(start);
            var posEnd = content.IndexOf(end, posStart + start.Length);
            if (posStart < 0 || posEnd < 0)
                return "";
            return content.Substring(posStart + start.Length, posEnd - posStart - start.Length);
        }
    }
}
