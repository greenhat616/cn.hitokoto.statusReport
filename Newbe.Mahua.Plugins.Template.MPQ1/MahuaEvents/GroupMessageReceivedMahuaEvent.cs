using Newbe.Mahua.MahuaEvents;
using Newbe.Mahua.Logging;
using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using IniParser;
using IniParser.Model;

using cn.hitokoto.statusReport.Drivers.Monitor;

namespace Newbe.Mahua.Plugins.Template.MPQ1.MahuaEvents {  
    public class hitokotoConstruct {
        public uint id { set; get; }
        public string hitokoto { set; get; }
        public string type { set; get; }
        public string from { set; get; }
        public string creator { set; get; }
        public uint created_at { set; get; }
    }

    public class TimerStart {
        static public bool TimerIsStart = false;
    }

    /// <summary>
    /// 群消息接收事件
    /// </summary>
    public class GroupMessageReceivedMahuaEvent : IGroupMessageReceivedMahuaEvent {
        private readonly IMahuaApi _mahuaApi;
        static monitor monitor;
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(GroupMessageReceivedMahuaEvent));
        protected uint[] adminList = new uint[5] {
            1943241505, // a632079
            342119543, // 飘飘
            442971704, // FreeJiShu
            458146168, // 酷儿
            3304242038 // 酷儿小号
        };

        public GroupMessageReceivedMahuaEvent(IMahuaApi mahuaApi) {
            _mahuaApi = mahuaApi;
        }

        public void StartTimer(string uri, int tickTime) {
            Logger.Debug("开始尝试激活定时器。");
            monitor = new monitor(uri, tickTime);
            TimerStart.TimerIsStart = true;
        }


        public void ShutdownTimer () {
            monitor.stopTimer();
            monitor = null;
            TimerStart.TimerIsStart = false;
        }

        static public string fetchHitokotoText () {
            Logger.Debug("开始尝试获取一言文本。");
            var request = (HttpWebRequest)WebRequest.Create("https://v1.hitokoto.cn/?encode=json");
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var data = JsonConvert.DeserializeObject<hitokotoConstruct>(responseString);
            Logger.Debug("已获取到一言文本。" + data.hitokoto);
            return data.hitokoto;
        }
        public void ProcessGroupMessage (GroupMessageReceivedContext context) {
            // Ping - Pong
            if (context.Message == "/ping") {
                _mahuaApi
                    .SendGroupMessage(context.FromGroup)
                    .At(context.FromQq)
                    .Newline()
                    .Text("Pong")
                    .Done();
            }

            // 初始化计时器
            int id = Array.IndexOf(adminList, uint.Parse(context.FromQq));
            if (id > -1) { // 如果是机器人管理员
                if (context.Message == "汇报监控状态") {
                    _mahuaApi
                        .SendGroupMessage(context.FromGroup)
                        .At(context.FromQq)
                        .Newline()
                        .Text(monitor.gernerateDataStatistics())
                        .Done();
                }

                if ((context.Message).ToString() == "启动状态监控") { // 由管理员发起启动指令， 然么启动循环计时器
                    string workDir = System.AppDomain.CurrentDomain.BaseDirectory; // 获取程序目录
                    string pluginConfigDir = workDir + "/Plugin/Config";
                    string configFile = pluginConfigDir + "/statusReport.settings.ini";
                    // 检测文件夹是否存在
                    if (!Directory.Exists(pluginConfigDir)) {
                        Directory.CreateDirectory(pluginConfigDir);
                    }
                    // 检测配置是否存在
                    if (!File.Exists(configFile)) {
                        // 不存在
                        _mahuaApi.SendGroupMessage(context.FromGroup)
                            .At(context.FromQq)
                            .Newline()
                            .Text("很抱歉， 机器人尚未配置。 请配置后再进行尝试！")
                            .Newline()
                            .Text(fetchHitokotoText())
                            .Newline()
                            .Text("现在时间: " + DateTime.Now.ToString("s"))
                            .Done();
                    } else {
                        if (!TimerStart.TimerIsStart) {
                            var parser = new FileIniDataParser();
                            int timeTick;
                            string uri;
                            try {
                                IniData data = parser.ReadFile(configFile);
                                var monitor = data["monitor"];
                                timeTick = int.Parse(monitor["tickTime"]);
                                uri = monitor["uri"] == "" ? "https://status.hitokoto.cn" : monitor["uri"];
                                StartTimer(uri, timeTick);
                            } catch (Exception err) {
                                Logger.Error(err.Message);
                                Logger.Debug("读取配置项失败， 开始使用默认配置激活计时器");
                                uri = "https://status.hitokoto.cn";
                                timeTick = 500;

                                StartTimer(uri, timeTick);
                            }
                            _mahuaApi.SendGroupMessage(context.FromGroup)
                                .At(context.FromQq)
                                .Newline()
                                .Text("监控服务已经成功启动！")
                                .Newline()
                                .Text("监控对象: " + uri)
                                .Newline()
                                .Text("监控间隔: " + timeTick + "ms")
                                .Newline()
                                .Text("您可以通过使用 \"汇报监控状态\" 来让机器人报告当前监控状态， 以及本地数据库中所储存得在线率等信息。")
                                .Newline()
                                .Text(fetchHitokotoText())
                                .Newline()
                                .Text("现在时间: " + DateTime.Now.ToString("s"))
                                .Done();
                        } else {
                            _mahuaApi.SendGroupMessage(context.FromGroup)
                                    .At(context.FromQq)
                                    .Newline()
                                    .Text("你是变态吗， 这还不满足？ 人家有好好开始啦 >_<")
                                    .Newline()
                                    .Text(fetchHitokotoText())
                                    .Newline()
                                    .Text("现在时间: " + DateTime.Now.ToString("s"))
                                    .Done();
                        }
                    }
                } else if ((context.Message).ToString() == "停止状态监控") { // 由管理员发起启动指令， 然么终止循环计时器
                    if (!TimerStart.TimerIsStart) {
                        Logger.Debug("触发事件: 停止状态监控。 目前状态: 已停止。无操作。");
                        _mahuaApi.SendGroupMessage(context.FromGroup)
                            .At(context.FromQq)
                            .Newline()
                            .Text("哦吼， 都没启动， 怎么停止啊。 再这样， 小心我口球你！")
                            .Newline()
                            .Text(fetchHitokotoText())
                            .Newline()
                            .Text("现在时间: " + DateTime.Now.ToString("s"))
                            .Done();
                    } else {
                        Logger.Debug("触发事件: 停止状态监控。 目前状态: 运行中。 停止状态监控。");
                        ShutdownTimer();
                        _mahuaApi.SendGroupMessage(context.FromGroup)
                            .At(context.FromQq)
                            .Newline()
                            .Text("监控服务已经停止。")
                            .Newline()
                            .Text(fetchHitokotoText())
                            .Newline()
                            .Text("现在时间: " + DateTime.Now.ToString("s"))
                            .Done();
                    }
                }
            }
            // throw new NotImplementedException();

            // 不要忘记在MahuaModule中注册
        }
    }
}
