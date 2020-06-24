using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using Native.Csharp.Sdk.Cqp.EventArgs;
using Native.Csharp.Sdk.Cqp.Interface;
using Freezer.Core;

namespace Native.Csharp.App.Event
{
    public class Event_GroupMsg : IReceiveGroupMessage
    {
        private readonly string PING_CMD = "$bot.ping";
        private readonly string PONG_CMD = "$bot.pong";
        private readonly string PA_CMD = "$bot.pa";

        private readonly string LCSC_CMD = "$bot.lcsc";

        private readonly string THB_SEARCH = "$bot.ths";
        private readonly string THB_QUERY = "$bot.thq";

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
                LCSCSearch(e, arg);
            }
            else if (e.Message.StartsWith(THB_SEARCH))
            {
                string arg = e.Message.Substring(THB_SEARCH.Length);
                arg = arg.Replace("\t", " ");
                while (arg.StartsWith(" "))
                    arg = arg.Substring(1);
                THBSearch(e, arg);
            }
            else if (e.Message.StartsWith(THB_QUERY))
            {
                string arg = e.Message.Substring(THB_QUERY.Length);
                arg = arg.Replace("\t", " ");
                while (arg.StartsWith(" "))
                    arg = arg.Substring(1);
                THBQuery(e, arg);
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

        public void LCSCSearch(CqGroupMessageEventArgs e, string arg)
        {
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

                try
                {
                    var screenshotJob = ScreenshotJobBuilder.Create("https://so.szlcsc.com/global.html?k=" + arg)
                    .SetBrowserSize(1920, 1080).SetCaptureZone(new CroppedZone(615, 730, 945, 301)).SetTrigger(new WindowLoadTrigger());
                    Image image = screenshotJob.Freeze();

                    var uuid = Guid.NewGuid().ToString("N");
                    image.Save("data/image/" + uuid + ".png");
                    string cqCode = Common.CqApi.CqCode_Image(uuid + ".png");
                    Common.CqApi.SendGroupMessage(e.FromGroup, cqCode);
                } catch (Exception ex)
                {
                    Common.CqApi.SendGroupMessage(e.FromGroup, "图片抓取出错（（（");
                }
            }
        }

        const string THB_SEARCH_CMD = "https://thwiki.cc/api.php?action=opensearch&format=xml&limit=5&search=";
        const string THB_QUERY_CMD = "https://thwiki.cc/api.php?action=query&format=xml&prop=extracts&exlimit=1&explaintext=1&exsectionformat=wiki&titles=";

        public void THBSearch(CqGroupMessageEventArgs e, string arg)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (WebClient client = new WebClient())
            {
                XmlDocument document = new XmlDocument();
                XmlReader reader = XmlReader.Create(client.OpenRead(THB_SEARCH_CMD + arg));
                document.Load(reader);

                var body = document.LastChild;
                var query = body.ChildNodes.Item(0);
                var section = body.ChildNodes.Item(1);

                string result = "搜索 " + query.InnerText + " 的结果如下！\n";
                foreach (XmlNode item in section.ChildNodes)
                {
                    var text = item.ChildNodes.Item(0).InnerText;
                    var url = item.ChildNodes.Item(1).InnerText;
                    string desc = "无";
                    if (item.ChildNodes.Count > 2)
                        desc = item.ChildNodes.Item(2).InnerText;
                    result += (
                        "----------------" + "\n" +
                        "● " + text + " (" + url + ")" + "\n" +
                        "简介：" + desc + "\n"
                    );
                }

                Common.CqApi.SendGroupMessage(e.FromGroup, result);
            }
        }

        public void THBQuery(CqGroupMessageEventArgs e, string arg)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (WebClient client = new WebClient())
            {
                XmlDocument document = new XmlDocument();
                XmlReader reader = XmlReader.Create(client.OpenRead(THB_QUERY_CMD + arg));
                document.Load(reader);

                var body = document.LastChild;
                var api = body.ChildNodes.Item(0);
                var pages = api.ChildNodes.Item(0);
                var page = pages.ChildNodes.Item(0);

                string result =
                    "----------------" + "\n" +
                    "● " + page.Attributes.GetNamedItem("title").InnerText + "\n" +
                    "----------------" + "\n";
                if (page.ChildNodes.Count > 0)
                {
                    var extract = page.ChildNodes.Item(0);
                    var text = extract.InnerText.Replace("\n\n", "\n");

                    result += (text + "\n");
                }
                else
                {
                    result += ("么有结果！" + "\n");
                }

                if (result.Length <= 128)
                    Common.CqApi.SendGroupMessage(e.FromGroup, result);
                else
                {
                    Common.CqApi.SendGroupMessage(e.FromGroup, result.Substring(0, 64) + " ... " + result.Substring(128, 64) + " ...");
                    try
                    {
                        var screenshotJob = ScreenshotJobBuilder.Create("https://thwiki.cc/" + arg)
                        .SetBrowserSize(512, 2048).SetCaptureZone(new CroppedZone(0, 0, 512, 2048)).SetTrigger(new WindowLoadTrigger());
                        Image image = screenshotJob.Freeze();

                        var uuid = Guid.NewGuid().ToString("N");
                        image.Save("data/image/" + uuid + ".png");
                        string cqCode = Common.CqApi.CqCode_Image(uuid + ".png");
                        Common.CqApi.SendGroupMessage(e.FromGroup, cqCode);
                    }
                    catch (Exception ex)
                    {
                        Common.CqApi.SendGroupMessage(e.FromGroup, "图片抓取出错（（（");
                    }
                }
            }
        }

    }
}
