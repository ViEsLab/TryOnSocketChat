using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;

namespace ChatoServer
{
    static class Program
    {
        static Socket serverSocket = null;
        static IPAddress ip = null;
        static IPEndPoint point = null;

        static Dictionary<string, Socket> allClientSockets = null;

        static MainForm form = null;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            allClientSockets = new Dictionary<string, Socket>();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bListenClick, bSendClick);
            Application.Run(form);

        }

        static EventHandler bListenClick = SetListen;
        static EventHandler bSendClick = SendMsg;

        static void SetListen(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(form.GetIPText());
            point = new IPEndPoint(ip, form.GetPort());

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                serverSocket.Bind(point);
                serverSocket.Listen(20);
                form.Println($"服务器开始在 {point} 上监听。");

                Thread thread = new Thread(Listen);
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start(serverSocket);
            }
            catch (Exception ex) {
                form.Println($"错误： {ex.Message}");
            }
        }

        static void Listen(object so)
        {
            Socket serverSocket = so as Socket;
            while (true)
            {
                try {
                    //等待连接并且创建一个负责通讯的socket
                    Socket clientSocket = serverSocket.Accept();
                    //获取链接的IP地址
                    string clientPoint = clientSocket.RemoteEndPoint.ToString();
                    form.Println($"{clientPoint} 上的客户端请求连接。");

                    allClientSockets.Add(clientPoint, clientSocket);
                    form.ComboBoxAddItem(clientPoint);
                    string msg = "ip=" + clientPoint;
                    byte[] sendee = Encoding.UTF8.GetBytes(msg);
                    foreach (string ip in allClientSockets.Keys)
                        if (ip != clientPoint)
                        {
                            allClientSockets[ip].Send(sendee);  //向所有客户端发送新客户端连接的ip信息
                            byte[] sendeeIP = Encoding.UTF8.GetBytes("ip=" + ip);
                            allClientSockets[clientPoint].Send(sendeeIP);  //向新客户端发送所有已有客户端的ip信息
                        }

                    //开启一个新线程不停接收消息
                    Thread thread = new Thread(Receive);
                    thread.IsBackground = true;
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start(clientSocket);
                }
                catch(Exception e) {
                    form.Println($"错误： {e.Message}");
                    break;
                }
            }
        }

        static void Receive(object so)
        {
            byte[] buf = new byte[1024 * 1024 * 2];
            byte[] cacheBuf = new byte[1024 * 1024 * 2];
            Socket clientSocket = so as Socket;
            string clientPoint = clientSocket.RemoteEndPoint.ToString();
            while (true) {
                try {
                    //获取发送过来的消息容器
                    int len = clientSocket.Receive(buf);
                    //有效字节为0则跳过
                    if (len == 0) break;

                    if (buf[0] == 1) //如果接收的字节数组的第一个字节是1，说明接收的是文件
                    {
                        form.Println("来自" + clientSocket.RemoteEndPoint + ": 的文件buf正在接收");
                        Buffer.BlockCopy(buf, 0, cacheBuf, 0, 1024 * 1024 * 2);
                        Thread thread = new Thread(Receive);
                        thread.IsBackground = true;
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start(clientSocket);
                    }
                    else if(buf[0] == 2)
                    {
                        form.Println("来自" + clientSocket.RemoteEndPoint + ": 的图片buf正在接收");
                        //发送并显示图片
                        MemoryStream ms = new MemoryStream();
                        ms.Write(buf, 1, len - 1);
                        Image img = Image.FromStream(ms);
                        form.SetPic(img);
                        form.Println("来自" + clientSocket.RemoteEndPoint + ": 的图片接收成功");
                    }
                    else
                    {
                        string s = Encoding.UTF8.GetString(buf, 0, len);
                        form.Println($"{clientPoint}: {s}");
                        if (s.StartsWith("message="))
                            foreach (String t in allClientSockets.Keys)
                            {
                                if (clientPoint != t)
                                {
                                    byte[] sendee = Encoding.UTF8.GetBytes($"{clientPoint}: {s}");
                                    allClientSockets[t].Send(sendee);
                                }
                            }
                        else if (s.StartsWith("ip="))
                        {
                            string ip = Regex.Match(s, @"\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}").Value;
                            byte[] sendee = Encoding.UTF8.GetBytes($"{clientPoint}: {s.Substring(3 + ip.Length)}");
                            allClientSockets[ip].Send(sendee);
                        }
                        if (s.StartsWith("TO="))
                        {
                            string ip = Regex.Match(s, @"\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}").Value;
                            allClientSockets[ip].Send(cacheBuf);
                        }
                    }

                    //byte[] sendee = Encoding.UTF8.GetBytes("服务器返回信息");
                    //clientSocket.Send(sendee);
                }
                catch (SocketException e) {
                    allClientSockets.Remove(clientPoint);
                    form.ComboBoxRemoveItem(clientPoint);  //移除服务端相应用户列表项

                    string msg = "Removeip=" + clientPoint;  //给所有客户端发送移除消息
                    byte[] sendee = Encoding.UTF8.GetBytes(msg);
                    foreach (string ip in allClientSockets.Keys)
                            allClientSockets[ip].Send(sendee);

                    form.Println($"客户端 {clientSocket.RemoteEndPoint} 中断连接： {e.Message}");
                    clientSocket.Close();
                    break;
                }
                catch(Exception e) {
                    form.Println($"错误： {e.Message}");
                }
            }
        }

        static void SendMsg(object sender, EventArgs e)
        {
            if(form.GetComboBoxItem() == null)
            {
                string msg = form.GetMsgText();
                if (msg == "") return;
                byte[] sendee = Encoding.UTF8.GetBytes($"服务器：{msg}");
                foreach (Socket s in allClientSockets.Values)
                    s.Send(sendee);
                form.Println(msg);
                form.ClearMsgText();
            }
            else
            {
                string msg = form.GetMsgText();
                if (msg == "") return;
                byte[] sendee = Encoding.UTF8.GetBytes($"服务器：{msg}");
                Socket socketSend = allClientSockets[form.GetComboBoxItem()];
                socketSend.Send(sendee);
                form.Println(msg);
                form.ClearMsgText();
            }
        }

    }
}
