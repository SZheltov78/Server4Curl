using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// 
// Клинеты отправляют файлы методом POST:
// curl -X POST -F "ИМЯ_КЛИЕНТА=@/var/Exchange/File.txt" XXX.XXX.XXX.XXX:80; 
//  Задача: 
//  Принять и разложить файлы по папкам (имя=имя клиента).
// 
namespace Server4Curl
{
    public partial class Form1 : Form
    {
        public List<Client> ClientList = new List<Client>();

        //класс клиента
        public class Client
        {
            public TcpClient TClient;
            public NetworkStream TStream;
            public int i = 0;
            public string Text = "";
            public bool Done = false;

            public Client()
            {
                TClient = Server.AcceptTcpClient();
                TStream = TClient.GetStream();                
            }

            public void Read()
            {
                if (TStream.DataAvailable)
                {
                    i = 0;
                    int l;
                    byte[] bytes = new byte[10000];
                    string data = "";
                    l = TStream.Read(bytes, 0, bytes.Length);                 
                    data = Encoding.ASCII.GetString(bytes, 0, l);
                    Text += data;
                }
                else
                {
                    //завершилась ли передача файла определить нельзя
                    i++;
                    if (!TClient.Connected) i = 100; //если только клиент не разорвал соединение
                }

                if (i > 5) //ожидается 5 циклов (5*500 = 2.5 сек) и дисконнект
                {
                    TStream.Close();
                    TClient.Close();
                    Done = true;
                }
            }
        }

       
        public void ToLog(string s)
        {
            var fs = File.AppendText("log.txt");
            s = DateTime.Now.ToString() + " - " + s;
            fs.WriteLine(s);
            fs.Close();
            Log_TB.AppendText(s + Environment.NewLine);
        }

        public Form1()
        {
            InitializeComponent();
        }

        public int port = Properties.Settings.Default.Server_Port;
        IPAddress localAddr = IPAddress.Parse(Properties.Settings.Default.Server_IP);
        public static TcpListener Server = null;


        private void Form1_Load(object sender, EventArgs e)
        {
            IP_TB.Text = Properties.Settings.Default.Server_IP;
            Port_TB.Text = Properties.Settings.Default.Server_Port.ToString();
            Path_TB.Text = Properties.Settings.Default.SavePath;

            ToLog("Start server.");
            Server = new TcpListener(localAddr, port);
            if (Server == null)
            {
                ToLog("Error of TcpListener.");
                return;
            }
            Server.Start();
            Accept_T.Enabled = true;
            Read_T.Enabled = true;
        }       

        //таймер принимает соединения
        private void Accept_T_Tick(object sender, EventArgs e)
        {
            if (Server.Pending())
            {
                ToLog("Client is accepted.");
                Client c = new Client();
                ClientList.Add(c);
                POST_TB.Text = "";
                VALUE_TB.Text = "";
            }

        }
        
        //таймер обрабатывает клиентов
        private void Read_T_Tick(object sender, EventArgs e)
        {
            foreach(Client cl in ClientList)
            {
                if (!cl.Done)
                {
                    cl.Read();
                }
                else
                {
                    //чтение завершено
                    ToLog("Read done.");

                    POST_TB.Text = cl.Text;

                    //парсинг 
                    string t = POST_TB.Lines[9];
                    string[] temp = t.Split('=');
                    t = temp[1];
                    temp = t.Split(';');
                    t = temp[0];
                    t = t.Replace("\"", "");

                    string path = t;
                    if (!Directory.Exists(Path_TB.Text + path)) Directory.CreateDirectory(Path_TB.Text + path);
                    
                    for(int c = 12; c < (POST_TB.Lines.Length - 2); c++)
                    {
                        VALUE_TB.AppendText(POST_TB.Lines[c] + Environment.NewLine);
                    }

                    File.WriteAllText(Path_TB.Text +  path+ @"\OUT.txt",VALUE_TB.Text);

                    ToLog($"Saved to {path}");
                    From_TB.Text = path;

                    //клиент больше не нужен
                    ClientList.Remove(cl);
                    return;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Server_IP= IP_TB.Text;
            Properties.Settings.Default.Server_Port = Convert.ToInt16( Port_TB.Text);
            Properties.Settings.Default.SavePath = Path_TB.Text;
            Properties.Settings.Default.Save();
            Application.Restart();
        }
    }
}
