using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace NetworkProject
{
    public partial class GameStart : Form
    {
        private static readonly int groupPort = 8000;
        public readonly UdpClient udp = new UdpClient(groupPort);
        private String serverIP;
        public GameStart()
        {
            InitializeComponent();
        }
        private void GameStart_Load(object sender, EventArgs e)
        {
            StartListening();
        }
        private void btnStartAsServer_Click(object sender, EventArgs e)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, groupPort);
            serverIP = GetLocalIPAddress();
            byte[] serverIPBytes = Encoding.ASCII.GetBytes(serverIP);
            udp.Send(serverIPBytes, serverIPBytes.Length, endPoint);
            Console.WriteLine("Broadcasted IP successfully\nServer IP: " + serverIP);
            GameSettingScreen gsc = new GameSettingScreen(true, null, udp);
            gsc.Show();
            this.Visible = false;
        }

        private void btnJoinAsClient_Click(object sender, EventArgs e)
        {
            IPAddress ip = IPAddress.Parse(serverIP);
            GameSettingScreen gsc = new GameSettingScreen(false, ip, null);
            gsc.Show();
            this.Visible = false;
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        private void Receive(IAsyncResult ar)
        {
            try
            {
                IPAddress add;
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, groupPort);
                byte[] bytes = udp.EndReceive(ar, ref ip);
                string message = Encoding.ASCII.GetString(bytes);
                if (message.StartsWith("Winner") && message != "" && message != null)
                {
                    try
                    {
                        Console.WriteLine("Received winner message : ");
                        int winnerRank = -1;
                        String winnerIP = "";
                        String[] messageParts = message.Split('#');
                        winnerIP = messageParts[1];
                        winnerRank = Int32.Parse(messageParts[2]);
                        Console.WriteLine("winner Ip is : " + winnerIP + " Rank is " + winnerRank);
                        this.Invoke((MethodInvoker)delegate
                        {
                            WinningForm form = new WinningForm(winnerRank, winnerIP);
                            form.Show();
                        });

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                if (message != "" && message != null && message == "start")
                {
                    Console.WriteLine("Received start game signal");
                    GameSettingScreen.startGame = true;
                }
                else if (message != "" && message != null && message.Contains(';'))
                {
                    Console.WriteLine("Received player list");
                    if (GameSettingScreen.thisForm != null)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            GameSettingScreen.thisForm.updatePlayerList(message.Split(';'));
                        });
                    }
                }
                if (message != "" && message != null && IPAddress.TryParse(message, out add))
                {
                    Console.WriteLine("Server IP Received: " + message);
                    serverIP = message;
                    this.Invoke((MethodInvoker)delegate
                    {
                        btnJoinAsClient.Enabled = true;
                        btnStartAsServer.Enabled = false;
                    });
                }
                //if(message != "start")
                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + '\n' + ex.Data + '\n' + ex.StackTrace);
            }
        }
        private void StartListening()
        {
            this.udp.BeginReceive(Receive, new object());
        }
    }
}
