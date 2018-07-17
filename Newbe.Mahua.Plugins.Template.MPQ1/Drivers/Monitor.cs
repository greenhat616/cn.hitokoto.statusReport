using Newbe.Mahua;
using Newbe.Mahua.Logging;
using Newbe.Mahua.MahuaEvents;
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
using cn.hitokoto.statusReport.Drivers.Hitokoto;
using cn.hitokoto.statusReport.Drivers.MailService;
using cn.hitokoto.statusReport.DbModels;

namespace cn.hitokoto.statusReport.Drivers.Monitor {
    public class hitokotoConstruct {
        public uint id { set; get; }
        public string hitokoto { set; get; }
        public string type { set; get; }
        public string from { set; get; }
        public string creator { set; get; }
        public uint created_at { set; get; }
    }

    public class monitor {
        static string uri;
        static Timer timer;
        static List<down> downServerList = new List<down>();
        static List<string> downServerIds = new List<string>();
        static List<string> serverIds = new List<string>(); // 所有节点的标识
        static string workDir = System.AppDomain.CurrentDomain.BaseDirectory; // 获取程序目录
        static string pluginConfigDir = workDir + "/Plugin/Config";
        static string configFile = pluginConfigDir + "/statusReport.settings.ini";
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(monitor));

        public class down {
            public string id { set; get; }
            public long startTS { set; get; }
        }

        static public string fetchHitokotoText() {
            Logger.Debug("开始尝试获取一言文本。");
            var request = WebRequest.Create("https://v1.hitokoto.cn/?encode=json");
            var response =  request.GetResponse();
            var responseString =  new StreamReader(response.GetResponseStream()).ReadToEnd();
            var data = JsonConvert.DeserializeObject<hitokotoConstruct>(responseString);
            Logger.Debug("已获取到一言文本。" + data.hitokoto);
            return data.hitokoto;
        }

        /// <summary>
        /// 转换时间戳为C#时间
        /// </summary>
        /// <param name="timeStamp">时间戳 单位：毫秒</param>
        /// <returns>C#时间</returns>
        public static DateTime ConvertTimeStampToDateTime(long timeStamp) {
            DateTime defaultTime = new DateTime(1970, 1, 1, 0, 0, 0);
            long defaultTick = defaultTime.Ticks;
            long timeTick = defaultTick + timeStamp * 10000;
            //// 东八区 要加上8个小时
            DateTime dt = new DateTime(timeTick).AddHours(8);
            return dt;
        }

        static public string gernerateDataStatistics() { // 读取数据库， 分析监控数据
            Logger.Debug("开始生成数据统计。");
            var context = new StatisticsContext();
            var result = "";
            DateTime now = DateTime.Now;

            // 首先， 检测是否当前有服务器宕机
            Logger.Debug("生成数据统计 -> 检测是否服务器存在故障。");
            if (downServerIds.ToArray().Length > 0) {
                result += "当前有节点故障， 故障列表如下:\r\n";
                Logger.Debug("生成数据统计 -> 有服务器存在故障。");

                foreach (var downServer in downServerList.ToArray()) {
                    // 计算持续时间
                    DateTime startTime = ConvertTimeStampToDateTime(downServer.startTS);
                    TimeSpan distance = now - startTime;
                    string last = distance.Days + "天" + distance.Hours + "时" + distance.Minutes + "分" + distance.Seconds + "秒";
                    string part = "标识: " + downServer.id + " -> 故障开始时间: " + startTime.ToString("s") + ", 已持续: " + last;
                    result += part + "\r\n";
                    Logger.Debug("生成数据统计 -> " + part);
                }
                result += "\r\n"; // 换行
            }

            // 生成统计报告
            Logger.Debug("生成数据统计 -> 生成统计报告");
            result += "统计报告: \r\n";
            var status = context.Status.ToList();
            foreach (var child in status) {
                var identification = child.identification;
                var total = child.totol;
                var up = child.up;
                var down = child.down;
                var onlineRate = Math.Round((double)(up / total) * 100, 2);
                var part = "标识: " + identification + " -> 总计:" + total + " 次, 正常: " + up + " 次, 故障: " + down + " 次; 在线率: " + onlineRate + "%";
                result += part + "\r\n";
                Logger.Debug("生成数据统计 -> " + part);
            }
            result += "\r\n"; // 换行
            result += fetchHitokotoText();

            Logger.Debug("生成数据统计 -> 完成.");
            // 返回结果
            return result;
        }

        private void saveStatus () { // 保存数据到状态监控
            Logger.Debug("开始保存状态统计");
            var context = new StatisticsContext();
            if (downServerIds.ToArray().Length > 0) { // 缓存存在故障数据
                Logger.Debug("保存数据统计 -> 发现缓存中存在故障数据");
                foreach (var id in downServerIds.ToArray()) { // 对缓存数组进行一次遍历， 然后更新对应数组的状态
                    var statusHasId = context.Status
                                        .Where(c => c.identification.Contains(id));
                    if (statusHasId.ToList().ToArray().Length > 0) { // 对于该标识有记录， 我们直接更新状态
                        Logger.Debug("保存数据统计 -> 故障 id: " + id + " 在数据库中发现记录");
                        var identification = statusHasId.First();
                        identification.totol++;
                        identification.down++;
                    } else { // 初始化
                        Logger.Debug("保存数据统计 -> 故障 id: " + id + "未在数据库中发现记录");
                        var row = new status() {
                            identification = id,
                            totol = 1,
                            down = 1,
                            up = 0
                        };
                        context.Status.Add(row);
                    }
                }
            }
            // 计算补集， 确认正常的服务
            Logger.Debug("保存数据统计 -> 计算补给， 获得正常标识数组");
            var diff = serverIds.ToArray().Where(c => !downServerIds.ToArray().Contains(c)).ToArray();
            foreach (var id in diff) {
                var statusHasId = context.Status
                                        .Where(c => c.identification.Contains(id));
                if (statusHasId.ToList().ToArray().Length > 0) { // 对于该标识有记录， 我们直接更新状态
                    Logger.Debug("保存数据统计 -> 正常 id: " + id + " 在数据库中发现记录");
                    var identification = statusHasId.First();
                    identification.totol++;
                    identification.up++;
                } else { // 初始化
                    Logger.Debug("保存数据统计 -> 正常 id: " + id + " 未在数据库中发现记录");
                    var row = new status() {
                        identification = id,
                        totol = 1,
                        down = 0,
                        up = 1
                    };
                    context.Status.Add(row);
                }
            }
            Logger.Debug("保存数据统计 -> 保存变更");
            context.SaveChanges();
            Logger.Debug("保存数据统计 -> 完成");
        }

        private void saveBuffer () { // 保存缓存。 懒的进行集合运算了， 所以直接清空表
            // 清空表
            Logger.Debug("开始保存缓存。");
            var context = new StatisticsContext();
            var buffer = context.Buffer.ToList();
            Logger.Debug("保存缓存 -> 删除所有记录");
            context.Buffer.RemoveRange(buffer);

            if (downServerIds.ToArray().Length > 0) {
                // 有数据可以缓存
                Logger.Debug("保存缓存 -> 存在数据可以缓存");
                foreach (var data in downServerList.ToArray()) {
                    Logger.Debug("保存缓存 -> 缓存 id: " + data.id + ", 开始时间戳: " + data.startTS);
                    var child = new buffer() {
                        identification = data.id,
                        startTS = data.startTS.ToString()
                    };
                    context.Buffer.Add(child);
                }
                Logger.Debug("保存缓存 -> 保存变更");
                context.SaveChanges();
                Logger.Debug("保存缓存 -> 完成");
            }
        }

        private void dumpData() { // 说是备份数据， 其实是保存数据进库
            saveStatus();
            saveBuffer();
        }

        private void addLog (string id, string type, long ts) { // 添加事件， 记录故障与恢复；
            var context = new StatisticsContext();
            var child = new log() {
                identification = id,
                type = type,
                ts = ts
            };
            context.Log.Add(child);
            context.SaveChanges();    
        }

        static void restoreData() { // 从 SQLite 数据库中恢复数据
            // 从缓存库中获取数据
            var context = new StatisticsContext();
            var buffer = context.Buffer.ToList().ToArray();
            if (buffer.Length > 0) {
                // 既然有数据， 那就有缓存。 执行操作恢复缓存
                foreach (var data in buffer) {
                    downServerIds.Add(data.identification); // 将标识添加到列表
                    var child = new down() {
                        id = data.identification,
                        startTS = long.Parse(data.startTS)
                    };
                    downServerList.Add(child);
                }
            }
        }

        private async Task<hitokotoStatusBody.Downserver[]> fetchDownServer(string URI) {
            WebRequest request = WebRequest.Create(URI);
            WebResponse response = await request.GetResponseAsync();
            string responseBody = await (new StreamReader(response.GetResponseStream()).ReadToEndAsync());
            // 解析 JSON
            var data = JsonConvert.DeserializeObject<hitokotoStatusBody.Rootobject>(responseBody);

            if (serverIds.ToArray().Length != data.children.Length) { // 更新 id 组
                serverIds.AddRange(data.children);
            }

            if (data.downServer != null) {
                return data.downServer;
            } else {
                return new hitokotoStatusBody.Downserver[0]; // 返回一个空数组
            }
        }

        private async void tickEvent(object sender, System.Timers.ElapsedEventArgs e) { // 计时器循环事件
            Logger.Debug("计时器循环事件开始执行。");
            // 获取宕机数据
            var downServers = await fetchDownServer(uri);
            Logger.Debug("成功获得宕机数据。");
            string[] diff;
            if (downServers.Length > 0) { // 存在宕机数据
                Logger.Debug("存在宕机事件。");

                // 初始化 ID 组， 缓存目前事件中宕机的 id
                List<string> existIdGroup = new List<string>();
                foreach (var downServer in downServers) {
                    // 缓存 id
                    existIdGroup.Add(downServer.id);

                    // 首先检测一下此 id 是否存在
                    var ids = downServerIds.ToArray();
                    if (Array.IndexOf(ids, downServer.id) == -1) { // 如果不存在， 那么就开始通知流程
                        Logger.Debug("标识:" + downServer.id + "， 初次发现宕机， 开始通知流程。");
                        // 首先，初始化 down
                        var child = new down() {
                            id = downServer.id,
                            startTS = downServer.startTs
                        };

                        var starTime = ConvertTimeStampToDateTime(child.startTS);

                        // 把记录写入缓存列表
                        downServerIds.Add(downServer.id);
                        downServerList.Add(child);

                        Logger.Debug("标识:" + downServer.id + "， 已经将信息写入缓存列表。");

                        // 发送通知任务
                        Logger.Debug("标识:" + downServer.id + " -> 开始读取配置项， 并执行配置映射。");
                        // 读取配置
                        var parser = new FileIniDataParser();
                        IniData config = parser.ReadFile(configFile);

                        // 映射配置
                        string[] groups = config["notification"]["group"].Split(',');
                        string[] mails = config["notification"]["mail"].Split(',');

                        // 1. 发送给 QQ 群组
                        Logger.Debug("标识:" + downServer.id + " -> 开始通知群组。");
                        foreach (var group in groups) {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                            Task.Factory.StartNew(() => {
                                using (var robotSession = MahuaRobotManager.Instance.CreateSession()) {
                                    var api = robotSession.MahuaApi;
                                    string msg = "警报, 一言节点出现故障!\r\n"
                                               + "节点标识: " + downServer.id + "\r\n"
                                               + "触发时间: " + starTime.ToString("s") + "\r\n"
                                               + fetchHitokotoText();
                                    api.SendGroupMessage(group, msg);
                                    Logger.Debug("标识:" + downServer.id + " -> 已通知群组: " + group);
                                }
                            });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                        }

                        // 2. 发送广播邮件
                        Logger.Debug("标识:" + downServer.id + " -> 开始邮件通知。");
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                        Task.Factory.StartNew(() => {
                            var smtp = new mailService();
                            var sub = "一言故障警告";
                            var msg = "警报， 一言节点出现故障。\r\n"
                                    + "节点标识: " + downServer.id + "\r\n"
                                    + "触发时间: " + starTime.ToString("s") + "\r\n"
                                    + fetchHitokotoText();
                            smtp.Send(mails, sub, msg);
                            Logger.Debug("标识:" + downServer.id + " -> 已通知邮件: " + mails.ToString());
                        });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                        Logger.Debug("标识:" + downServer.id + " -> 同步任务已经完成， 请等待异步程序执行完毕。");
                    }
                }

                // 求当前宕机列表和缓存列表的补集， 然后通知
                Logger.Debug("正在求当前宕机列表和缓存列表的补集...");
                diff = downServerIds.ToArray().Where(c => !existIdGroup.ToArray().Contains(c)).ToArray();
            } else {
                diff = downServerIds.ToArray(); // 既然宕机列表不存在， 说明所有节点正常。 如果缓存存在内容， 全部释放。
            }

            if (diff.Length > 0) { // 缓存区 ids 多于目前宕机的 ids， 所以有节点恢复了。 发生恢复行为
                Logger.Debug("补集不为空集， 进行恢复事件。");
                foreach (var id in diff) {
                    DateTime startTime = DateTime.Now;
                    // 从缓存区移除
                    Logger.Debug("标识:" + id + " -> 从缓存区中移除纪录。");
                    downServerIds.RemoveAll(c => downServerIds.Contains(c));
                    for (int i = downServerList.Count - 1; i >= 0; i--) {
                        if (downServerList[i].id == id) {
                            startTime = ConvertTimeStampToDateTime(downServerList[i].startTS);
                            downServerList.RemoveAt(i);
                        }
                    }
                    // 获取持续时间
                    var now = DateTime.Now;
                    var distance = now - startTime;
                    var last = distance.Days + "天" + distance.Hours + "时" + distance.Minutes + "分" + distance.Seconds + "秒";

                    Logger.Debug("标识:" + id + " -> 开始读取配置项， 并执行配置映射。");
                    // 读取配置
                    var parser = new FileIniDataParser();
                    IniData config = parser.ReadFile(configFile);

                    // 映射配置
                    string[] groups = config["notification"]["group"].Split(',');
                    string[] mails = config["notification"]["mail"].Split(',');

                    // 1. 发送给 QQ 群组
                    Logger.Debug("标识:" + id + " -> 开始通知群组。");
                    foreach (var group in groups) {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                        Task.Factory.StartNew(() => {
                            using (var robotSession = MahuaRobotManager.Instance.CreateSession()) {
                                var api = robotSession.MahuaApi;
                                string msg = "一言节点已从故障中恢复。\r\n"
                                           + "节点标识: " + id + "\r\n"
                                           + "持续时间: " + last + "\r\n"
                                           + fetchHitokotoText();
                                api.SendGroupMessage(group, msg);
                                Logger.Debug("标识:" + id + " -> 已通知群组: " + group);
                            }
                        });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    }

                    // 2. 发送广播邮件
                    Logger.Debug("标识:" + id + " -> 开始邮件通知。");
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    Task.Factory.StartNew(() => {
                        var smtp = new mailService();
                        var sub = "一言已从故障中恢复";
                        var msg = "一言节点已从故障中恢复。\r\n"
                                + "节点标识: " + id + "\r\n"
                                + "持续时间: " + last + "\r\n"
                                + fetchHitokotoText();
                        smtp.Send(mails, sub, msg);
                        Logger.Debug("标识:" + id + " -> 已通知邮件: " + mails.ToString());
                    });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                    Logger.Debug("标识:" + id + " -> 同步任务已经完成， 请等待异步程序执行完毕。");
                }
            }

        }

        public void stopTimer() {
            timer.Stop();
            timer.Dispose();
        }

        public monitor(string URI, int tickTime) { // 构造函数 启动计时器线程
            Logger.Debug("启动计时器.... 间隔:" + tickTime + "ms");
            restoreData();
            uri = URI;
            timer = new Timer(tickTime);
            timer.Elapsed += new ElapsedEventHandler(tickEvent);
            timer.Start();
        }

    }
}
