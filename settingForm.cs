using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;
using MailKit.Net.Smtp;
using MimeKit;

namespace Newbe.Mahua.Plugins.Template.MPQ1 {
    public partial class settingForm : Form {
        static private string workDir = System.AppDomain.CurrentDomain.BaseDirectory; // 获取程序目录
        static private string pluginConfigDir = workDir + "/Plugin/Config";
        static private string configFile = pluginConfigDir + "/statusReport.settings.ini";

        public settingForm() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            // 窗口载入 读配置项
            if (!Directory.Exists(pluginConfigDir)) {
                Directory.CreateDirectory(pluginConfigDir);
            }

            if (File.Exists(configFile)) {
                // 初始化解析器
                var parser = new FileIniDataParser();
                try {
                    IniData data = parser.ReadFile(configFile);
                    // 导入配置
                    textBox1.Text = data["monitor"]["uri"] != "" ? data["monitor"]["uri"] : textBox1.Text;
                    textBox2.Text = data["monitor"]["tickTime"] != "" ? data["monitor"]["tickTime"] : textBox2.Text;

                    textBox3.Text = data["smtp"]["port"] != "" ? data["smtp"]["port"] : textBox3.Text;
                    textBox4.Text = data["smtp"]["username"] != "" ? data["smtp"]["username"] : textBox4.Text;
                    textBox5.Text = data["smtp"]["password"] != "" ? data["smtp"]["password"] : textBox5.Text;
                    textBox6.Text = data["smtp"]["host"] != "" ? data["smtp"]["host"] : textBox6.Text;
                    checkBox1.Checked = data["smtp"]["enableSSL"] != "" ? bool.Parse(data["smtp"]["enableSSL"]) : checkBox1.Checked;

                    textBox7.Text = data["notification"]["mail"] != "" ? data["notification"]["mail"] : textBox7.Text;
                    textBox8.Text = data["notification"]["group"] != "" ? data["notification"]["group"] : textBox8.Text;

                } catch (InvalidCastException err) { // 说是捕获错误， 其实是丢掉所有错误
                    Console.WriteLine(err);
                }
            }
        }

        private void saveConfig() {
            if (!Directory.Exists(pluginConfigDir)) {
                Directory.CreateDirectory(pluginConfigDir);
            }

            if (!File.Exists(configFile)) {
                File.Create(configFile);
            }

            // 保存配置到配置项文件
            // 初始化解析器
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(configFile);

            // 监控配置
            data["monitor"]["uri"] = textBox1.Text;
            data["monitor"]["tickTime"] = textBox2.Text;

            // SMTP 配置
            data["smtp"]["host"] = textBox6.Text;
            data["smtp"]["port"] = textBox3.Text;
            data["smtp"]["username"] = textBox4.Text;
            data["smtp"]["password"] = textBox5.Text;
            data["smtp"]["enableSSL"] = checkBox1.Checked.ToString();

            // 通知配置
            data["notification"]["mail"] = textBox7.Text;
            data["notification"]["group"] = textBox8.Text;

            // 写入
            parser.WriteFile(configFile, data);
        }

        private void label2_Click(object sender, EventArgs e) {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            saveConfig();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e) {
            // 测试 SMTP 链接
            // 映射信息
            string host = textBox6.Text;
            int port = int.Parse(textBox3.Text);
            string username = textBox4.Text;
            string password = textBox5.Text;
            bool enableSSL = checkBox1.Checked;
            var mail = new MimeMessage();
            mail.From.Add(new MailboxAddress(username));
            mail.To.Add(new MailboxAddress("a632079@qq.com"));
            mail.Subject = "Test Connection";
            mail.Body = new TextPart("plain") {
                Text = "Just Test Mail Connection"
            };
            try {
                var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, a) => true;
                client.Connect(host, port, enableSSL);
                client.Authenticate(username, password);
                client.Send(mail);
                MessageBox.Show("连接成功");
            } catch (ArgumentException err) {
                MessageBox.Show("连接失败， 错误信息请查看错误堆栈！");
                throw err;
            } catch (Exception err) {
                MessageBox.Show("连接失败， 错误信息请查看错误堆栈！");
                throw err;
            }

        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.')) {
                e.Handled = true;
            }
        }
    }
}
