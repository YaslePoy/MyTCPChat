using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyChatServer
{
    internal class Program
    {
        static List<User> Users;
        static IPEndPoint Location;
        static Dictionary<string, List<User>> Chats;
        static Dictionary<User, string> SelectedChats;
        static void Main(string[] args)
        {
            Users = new();
            Chats = new();
            SelectedChats = new();
            var config = File.ReadAllLines("config.txt");
            Location = new IPEndPoint(IPAddress.Parse(config[0]), int.Parse(config[1]));
            TcpListener ls = new TcpListener(Location);
            ls.Start();

            while (true)
            {
                try
                {
                    var client = ls.AcceptTcpClient();
                    var clStream = client.GetStream();
                    if (clStream.CanRead)
                    {
                        byte[] myReadBuffer = new byte[1024];
                        StringBuilder myCompleteMessage = new StringBuilder();
                        int numberOfBytesRead = 0;

                        numberOfBytesRead = clStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                        myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));
                        var initMsg = myCompleteMessage.ToString().Split(" ");
                        var newUser = new User(initMsg[1], clStream);
                        if (Users.Any(i => i.Name == initMsg[1]))
                        {
                            newUser.SendMessage("name is occupied");
                            newUser.Stream.Close();
                        }
                        else
                        {
                            Console.WriteLine($"new connection: {newUser.Name}");
                            newUser.NewMessage += OnGetMessage;
                            Task.Run(newUser.StartListen);
                            Users.Add(newUser);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Exception");
                }
            }

            void OnGetMessage(object sender, string data)
            {
                var user = sender as User;
                Console.WriteLine($"User [{user.Name}] : {data}");
                if (data.StartsWith("/"))
                {
                    var commands = data.Split(' ');
                    switch (commands[0])
                    {
                        case "/bye":
                            user.Stop();
                            Console.WriteLine($"{user.Name} disconnected");
                            break;
                        case "/chat":
                            if (commands[1] == "join")
                            {
                                Chats.TryAdd(commands[2], new List<User>());
                                Chats[commands[2]].Add(user);
                                if (!SelectedChats.TryAdd(user, commands[2]))
                                    SelectedChats[user] = commands[2];
                            }
                            else
                            {
                                if (!Chats.ContainsKey(commands[2]))
                                    break;
                                Chats[commands[2]].Remove(user);
                                SelectedChats.Remove(user);
                            }


                            break;
                    }
                }
                else
                {
                    if (!SelectedChats.ContainsKey(user))
                        return;
                    var chat = SelectedChats[user];
                    var members = Chats[chat];
                    members.Remove(user);
                    foreach (var member in members)
                    {
                        member.SendMessage($"/msg {chat} {user.Name} {data}");
                    }
                }

            }
        }
    }
    public class User
    {
        public string Name;
        public NetworkStream Stream;
        public bool IsNewMessage => Stream.DataAvailable;
        public string LastMessage;
        bool working = true;
        public User(string name, NetworkStream stream)
        {
            Name = name;
            Stream = stream;
        }
        public void StartListen()
        {
            while (working)
            {
                var sb = new StringBuilder();
                int n = 0;
                while (true)
                {
                    sb.Clear();
                    var inMsg = new byte[1025];
                    n = Stream.Read(inMsg, 0, inMsg.Length);
                    sb.Append(Encoding.UTF8.GetString(inMsg, 0, n));
                    LastMessage = sb.ToString();
                    NewMessage?.Invoke(this, LastMessage);
                }
            }
        }
        public void Stop()
        {
            working = false;
        }

        public void SendMessage(string message)
        {
            var sendData = Encoding.UTF8.GetBytes(message);
            Stream.Write(sendData, 0, sendData.Length);
        }
        public event EventHandler<string> NewMessage;

    }
}
