using System.IO;
using System.Net;
using MyChatServer;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualBasic;

namespace MyChatClient
{
    internal class Program
    {
        static User ServerIO;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (o, e) => ServerIO.SendMessage("/bye");
            var config = File.ReadAllLines("config.txt");
            IPEndPoint ServerLoc = new IPEndPoint(IPAddress.Parse(config[0]), int.Parse(config[1]));
            TcpClient Server = new TcpClient(new IPEndPoint(IPAddress.Any, int.Parse(config[2])));
            try
            {
                Server.Connect(ServerLoc);
                var serverStream = Server.GetStream();
                ServerIO = new User("server", serverStream);
                var initMsg = "init " + config[3];
                ServerIO.SendMessage(initMsg);
                while (true)
                {
                    var newMsg = Console.ReadLine();
                    ServerIO.SendMessage(newMsg);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static void HandleServer(object sender, string data)
        {

        }
    }
}
