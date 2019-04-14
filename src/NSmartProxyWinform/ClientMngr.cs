﻿using log4net;
using NSmartProxy.Client;
using NSmartProxy.Data;
using NSmartProxy.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Repository;
using Newtonsoft.Json;
using NSmartProxy.Infrastructure;

namespace NSmartProxyWinform
{
    public partial class ClientMngr : Form
    {
        Router clientRouter;
        private Log4netLogger logger;
        private bool configChanged = false;
        private Config config;

        private const string NULL_CLIENT_TEXT = "<未编辑节点>";
        private const string RANDOM_PORT_TEXT = "<随机>";
        private const string START_TAG_TEXT = "1";
        private const string END_TAG_TEXT = "0";
        public bool IsStarted
        {
            get => btnStart.Tag.ToString() == END_TAG_TEXT;
        }

        public ClientMngr()
        {
            InitializeComponent();
            //将日志写入窗体中。
            logger = new Log4netLogger();
            logger.BeforeWriteLog = (msg) => { ShowInfo(msg.ToString()); };
            //右下角小图标
            notifyIconNSPClient.Icon = Properties.Resources.servicestopped;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (ValidateConfig() && SaveFormDataToConfigFile())
            {
                StartOrStop();
            }
        }

        private bool ValidateConfig()
        {
            bool isValid = true;
            if (tbxProviderAddr.Text == "")
            {
                errorProvider1.SetError(tbxProviderAddr, "必须填写服务器地址");
                isValid = false;
            }

            if (ValidateRequired(tbxProviderAddr) &&
                ValidateRequired(tbxConfigPort) &&
                ValidateRequired(tbxReversePort) &&
                ValidateMoreThanZero(tbxConfigPort) &&
                ValidateMoreThanZero(tbxReversePort))
            {
                isValid = true;
            }
            else
            {
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateMoreThanZero(Control ctrl)
        {
            if (int.Parse(ctrl.Text) < 1)
            {
                errorProvider1.SetError(ctrl, "值必须大于0");
                return false;
            }
            return true;

        }

        private bool ValidateRequired(Control ctrl)
        {
            if (ctrl.Text == "")
            {
                errorProvider1.SetError(ctrl, "必填");
                return false;
            }
            return true;
        }

        private void StartOrStop()
        {
            btnStart.Enabled = false;
            Task tsk;
            if (btnStart.Tag.ToString() == START_TAG_TEXT)
            {
                notifyIconNSPClient.BalloonTipText = "内网穿透已启动";
                listBox1.ForeColor = Color.Green;
                foreach (var item in listBox1.Items)
                {
                    notifyIconNSPClient.BalloonTipText += "\r\n" + item.ToString();
                }
                notifyIconNSPClient.ShowBalloonTip(5000);
                tsk = StartClientRouter(config);
                btnStart.Text = "停止";
                btnStart.Tag = END_TAG_TEXT;
            }
            else
            {
                listBox1.ForeColor = Color.Black;
                tsk = clientRouter.Close();
                btnStart.Text = "开始";
                btnStart.Tag = START_TAG_TEXT;
            }

            Task tskWait = Task.Delay(10000);
            tsk.ContinueWith(_ => btnStart.Invoke(new Action(
                () =>
                {
                    btnStart.Enabled = true;
                    if (IsStarted)
                    {
                        notifyIconNSPClient.Icon = Properties.Resources.servicerunning;
                    }
                    else
                    {
                        notifyIconNSPClient.Icon = Properties.Resources.servicestopped;
                    }
                }
            )));
        }

        private async Task StartClientRouter(Config config)
        {
            clientRouter = new Router(logger);
            clientRouter.DoServerNoResponse = () =>
            {
                clientRouter.Close().ConfigureAwait(false);
                logger.Info("服务器无响应，五秒后重试...");
                Thread.Sleep(5000);
                StartClientRouter(config);
            };

            //read config from config file.
            SetConfig(clientRouter, config);// clientRouter.SetConifiguration();
            var tsk = clientRouter.ConnectToProvider();
            await tsk.ConfigureAwait(false);
        }

        private void SetConfig(Router clientRouter, Config config)
        {
            clientRouter.SetConifiguration(config);
        }

        public void ShowInfo(string info)
        {
            tbxLog.Invoke(
                new Action(() =>
                {
                    tbxLog.AppendText(info);
                    tbxLog.AppendText(Environment.NewLine);
                    tbxLog.ScrollToCaret();
                }
            ));
        }

        private void ShowInExplorer_Click(object sender, EventArgs e)
        {
            var appender = LogManager.GetRepository(Program.LOGGER_REPO_NAME).GetAppenders()
                .Where((o) => o.GetType() == typeof(FileAppender)).First();
            var filePath = ((FileAppender)appender).File;

            //记录日志
            string argument = "/select, \"" + filePath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            ExitProgram();
        }

        private void ExitProgram()
        {
            if (MessageBox.Show("确认退出？", "NSmartProxy", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                notifyIconNSPClient.Dispose();
                Environment.Exit(0);
            }
        }

        private void ClientMngr_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void notifyIconNSPClient_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void ShowForm()
        {
            this.Show();
            this.TopMost = true;
            Application.DoEvents();
            this.TopMost = false;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem?.ToString() == NULL_CLIENT_TEXT)
            {
                tbxPort.Clear();
                tbxTargetServerAddr.Clear();
                tbxTargetServerPort.Clear();
            }
            else//节点配置显示
            if (listBox1.SelectedItem != null)
            {
                var strSelectedItemStr = listBox1.SelectedItem.ToString();
                var strParts = strSelectedItemStr.Split(new string[]
                {
                    "=>", ":"
                }, StringSplitOptions.None);
                if (strParts.Length != 4)
                {
                    MessageBox.Show("非法选择项");
                    return;
                }

                tbxPort.Text = strParts[1].Trim();
                tbxTargetServerAddr.Text = strParts[2].Trim();
                tbxTargetServerPort.Text = strParts[3].Trim();
            }
        }



        private void 退出程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExitProgram();
        }

        private void ConfigValueChanged(object sender, EventArgs e)
        {
            configChanged = true;
        }

        private void listBox1_Leave(object sender, EventArgs e)
        {
            listBox1.BackColor = Color.White;
        }

        private void btnAddClient_Click(object sender, EventArgs e)
        {
            int index = listBox1.Items.Add(NULL_CLIENT_TEXT);
            listBox1.SelectedIndex = index;
        }



        private void targetServer_TextChanged(object sender, EventArgs e)
        {
            printTextToList();
        }

        private void printTextToList()
        {
            if (listBox1.SelectedItem != null)
            {
                int originIndex = listBox1.SelectedIndex;
                listBox1.Items.Remove(listBox1.SelectedItem);

                listBox1.Items.Insert(originIndex,
                    $@"{tbxProviderAddr.Text}:{tbxPort.Text}  => {tbxTargetServerAddr.Text}:{tbxTargetServerPort.Text}");
                listBox1.SelectedIndex = originIndex;
            }

            configChanged = true;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int originIndex = listBox1.SelectedIndex;
            listBox1.Items.Remove(listBox1.SelectedItem);
            if (originIndex < listBox1.Items.Count)
                listBox1.SelectedIndex = originIndex;
            configChanged = true;
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                listBox1.SelectedIndex =
                    listBox1.Items.Add(listBox1.SelectedItem.ToString());
            }
        }


        private void tbxTargetServerPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void 启动内网穿透ToolStripMenuItem_Paint(object sender, PaintEventArgs e)
        {
            if (IsStarted)
            {
                this.启动内网穿透ToolStripMenuItem.Image = global::NSmartProxyWinform.Properties.Resources.base_checkmark_32;
            }
            else
            {
                this.启动内网穿透ToolStripMenuItem.Image = null;
            }
        }

        private void 启动内网穿透ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //StartOrStop();
            btnStart_Click(sender, e);
        }

        private void 配置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshFormFormConfig();
        }

        private void RefreshFormFormConfig()
        {
            Config conf = ConfigHelper.ReadAllConfig(Program.CONFIG_FILE_PATH);
            tbxProviderAddr.Text = conf.ProviderAddress;
            tbxConfigPort.Text = conf.ProviderConfigPort.ToString();
            tbxReversePort.Text = conf.ProviderPort.ToString();
            //if(tbxReversePort.Text == "" ? tbxReversePort.Text = "0";

            listBox1.Items.Clear();
            foreach (var confClient in conf.Clients)
            {
                listBox1.Items.Add(
                    $@"{tbxProviderAddr.Text}:{confClient.ConsumerPort}  => {confClient.IP}:{confClient.TargetServicePort}");
            }
        }


        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            if (ValidateConfig())
                SaveFormDataToConfigFile();
            MessageBox.Show("保存成功");
        }

        private bool SaveFormDataToConfigFile()
        {
            config = new Config();
            //1.刷新配置
            config.ProviderAddress = tbxProviderAddr.Text;
            config.ProviderConfigPort = int.Parse(tbxConfigPort.Text);
            config.ProviderPort = int.Parse(tbxReversePort.Text);
            //2.保存配置到文件
            foreach (var item in listBox1.Items)
            {
                var strParts = item.ToString().Split(new string[]
                {
                    "=>", ":"
                }, StringSplitOptions.None);
                if (strParts.Length != 4)
                {
                    MessageBox.Show("节点列表数据非法");
                    listBox1.BackColor = Color.LightCoral;

                    return false;
                }
                config.Clients.Add(new ClientApp
                {
                    ConsumerPort = Convert.ToInt32(strParts[1]),
                    IP = strParts[2].Trim(),
                    TargetServicePort = Convert.ToInt32(strParts[3])
                });
            }
            config.SaveChanges(Program.CONFIG_FILE_PATH);
            return true;
        }

        private void ClientMngr_Load(object sender, EventArgs e)
        {
            RefreshFormFormConfig();
        }
    }
}
