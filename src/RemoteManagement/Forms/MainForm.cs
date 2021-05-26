using RavenBot.Core;
using RemoteManagement.Forms;
using Shinobytes.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteManagement
{
    public partial class MainForm : Form
    {
        private readonly IoC ioc;
        private ILogger logger;
        private IClient client;
        private IServerPacketSerializer packetSerializer;
        private RefreshableDataDialog activeDialog;

        // have ioc null as default so the designer doesnt go bananas.
        public MainForm(IoC ioc)
        {
            this.ioc = ioc;
            InitializeComponent();

            if (ioc == null) return;
            this.FormClosing += MainForm_FormClosing;
            this.logger = ioc.Resolve<ILogger>();

            var serverSettings = ioc.Resolve<ServerSettings>();
            packetSerializer = ioc.Resolve<IServerPacketSerializer>();
            inputHost.Text = serverSettings.host;
            inputPort.Text = serverSettings.port.ToString();

            SetupClient();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.client.Dispose();
        }

        private void SetupClient()
        {
            this.client = ioc.Resolve<IClient>();
            this.client.DataReceived += Client_DataReceived;
        }

        private async void Connect()
        {
            var serverSettings = ioc.Resolve<ServerSettings>();
            var host = string.IsNullOrEmpty(inputHost.Text) ? serverSettings.host : inputHost.Text;
            var port = string.IsNullOrEmpty(inputPort.Text) ? serverSettings.port : int.Parse(inputPort.Text);

            if (await this.client.ConnectAsync(host, port, CancellationToken.None))
            {
                SetStatusText("Client Connected");
                AppendLogLine("[CLIENT] Connected");

                SendPacketType(PacketTypes.Hello);
            }
            else
            {
                SetStatusText("Client Connection Failed");
                AppendLogLine("Client connection failed");
            }
        }

        private void SendPacketType(string type)
        {
            var packet = packetSerializer.Serialize(new ServerPacket(type));
            if (packet != null)
            {
                this.SendPacket(packet);
            }
        }

        private void SendPacket(DataPacket packet)
        {
            this.client.Send(packet.Buffer, packet.Offset, packet.Length);
        }

        private void Disconnect()
        {
            if (client.Connected)
            {
                try
                {
                    this.client.Dispose();
                }
                catch { }
                SetupClient();
                SetStatusText("Client Disconnected");
                AppendLogLine("[CLIENT] Disconnected");
            }
        }

        private void Client_DataReceived(object sender, DataPacket e)
        {
            logger.WriteDebug("Client Received Data: " + e.Length);
            var data = UTF8Encoding.UTF8.GetString(e.Buffer, e.Offset, e.Length);
            //var dataRows = data.Split('\n');
            //foreach (var row in dataRows)
            //{
            //    var rowData = row.Replace("\r", "");

            //    if (!rowData.StartsWith("["))
            //    {
            //        // raw data received
            //        if (rowData.Contains("=+"))
            //        {
            //            // begin
            //            if (activeDialog != null)
            //            {
            //                activeDialog.AwaitingData = true;
            //                activeDialog.ClearData();
            //            }
            //        }
            //        else if (rowData.Contains("=-"))
            //        {
            //            if (activeDialog != null)
            //            {
            //                activeDialog.AwaitingData = false;
            //            }
            //            // end
            //        }
            //        else if (activeDialog != null && activeDialog.AwaitingData)
            //        {
            //            activeDialog.AddData(rowData);
            //        }

            //    }
            //}
            AppendLog("<-- " + data);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (!client.Connected)
            {
                return;
            }

            SendPacketType(PacketTypes.SessionList);

            //activeDialog = new SessionList(() => SendPacketType(PacketTypes.SessionList));
            //activeDialog.ShowDialog();
            //activeDialog = null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!client.Connected)
            {
                return;
            }

            SendPacketType(PacketTypes.ConnectionList);

            //activeDialog = new ConnectionList(() => SendPacketType(PacketTypes.ConnectionList));
            //activeDialog.ShowDialog();
            //activeDialog = null;
        }

        private void SetStatusText(string text)
        {
            BeginInvoke(() =>
            {
                lblStatus.Text = text;
            });
        }

        private void AppendLog(string message)
        {
            BeginInvoke(() =>
            {
                txtLog.AppendText("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
            });
        }
        private void AppendLogLine(string message)
        {
            BeginInvoke(() =>
            {
                txtLog.AppendText("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine);
            });
        }

        private void BeginInvoke(Action a)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ActionInvoke(BeginInvoke), a);
            }
            else if (a != null)
            {
                a();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Save log";
                sfd.Filter = "Text File (*.txt)|*.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(sfd.FileName, txtLog.Text);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
    public delegate void ActionInvoke(Action a);

}
