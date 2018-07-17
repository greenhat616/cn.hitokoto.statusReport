using Newbe.Mahua.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using IniParser;
using IniParser.Model;

namespace cn.hitokoto.statusReport.Drivers.MailService {
    public class mailService {
        private ILog Logger = LogProvider.GetLogger(typeof(mailService));
        protected string host;
        protected int port;
        protected string username;
        protected string password;
        protected bool enableSSL;
        protected SmtpClient client;
        static string workDir = System.AppDomain.CurrentDomain.BaseDirectory; // 获取程序目录
        static string pluginConfigDir = workDir + "/Plugin/Config";
        static string configFile = pluginConfigDir + "/statusReport.settings.ini";

        public bool Send(string[] to, string subject, string body) {
            try {
                initSmtpService();
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(username));
                // 迭代添加 TO
                foreach (var _ in to) {
                    message.To.Add(new MailboxAddress(_));
                }
                message.Subject = subject;
                message.Body = new TextPart("plain") {
                    Text = body
                };
                client.Send(message);
                closeSmtp();
                return true;
            } catch (Exception err) {
                Console.WriteLine(err);
                return false;
            }
        }

        protected void initSmtpService() {
            client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(host, port, enableSSL);
            client.Authenticate(username, password);
        }

        protected void closeSmtp() {
            client.Disconnect(true);
        }
        public mailService() {
            // 获取配置
            var parser = new FileIniDataParser();
            IniData config = parser.ReadFile(configFile);

            // 配置映射
            host = config["smtp"]["host"];
            port = int.Parse(config["smtp"]["port"]);
            username = config["smtp"]["username"];
            password = config["smtp"]["password"];
            enableSSL = bool.Parse(config["smtp"]["enableSSL"]);
        }
    }
}
