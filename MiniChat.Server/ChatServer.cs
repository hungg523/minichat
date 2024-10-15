using System.Net;
using System.Net.Sockets;
using System.Text;

class ChatServer
{
    private static TcpListener server;
    private static List<ClientHandler> clients = new List<ClientHandler>();
    private static List<string> chatHistory = new List<string>(); // Lưu trữ lịch sử chat chung
    private static Dictionary<string, List<string>> privateChatHistory = new Dictionary<string, List<string>>(); // Lưu trữ lịch sử chat riêng

    static void Main()
    {
        server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            ClientHandler clientHandler = new ClientHandler(client);
            clients.Add(clientHandler);
            Thread clientThread = new Thread(clientHandler.Run);
            clientThread.Start();
        }
    }

    public static void Broadcast(string message, string senderName, string groupName = null)
    {
        chatHistory.Add(message);

        if (groupName == null)
        {
            // Gửi đến tất cả các client
            foreach (ClientHandler client in clients)
            {
                client.SendMessage(message);
            }
        }
    }

    public static void SendPrivateMessage(string message, string receiverName, string senderName)
    {
        string formattedMessage = $"[Private from {senderName} to {receiverName}]: {message}";

        // Thêm tin nhắn vào lịch sử chat riêng giữa người gửi và người nhận
        string key = GetPrivateChatKey(senderName, receiverName);
        if (!privateChatHistory.ContainsKey(key))
        {
            privateChatHistory[key] = new List<string>();
        }
        privateChatHistory[key].Add(formattedMessage);

        // Gửi tin nhắn đến người gửi và người nhận
        foreach (ClientHandler client in clients)
        {
            if (client.ClientName == receiverName || client.ClientName == senderName)
            {
                client.SendMessage(formattedMessage);
            }
        }
    }

    public static void SendChatHistory(ClientHandler client)
    {
        // Gửi toàn bộ lịch sử chat công khai đến client mới
        foreach (string message in chatHistory)
        {
            client.SendMessage(message);
        }

        // Gửi lịch sử chat riêng cho người dùng hiện tại
        foreach (var entry in privateChatHistory)
        {
            string[] participants = entry.Key.Split('-');
            if (Array.Exists(participants, p => p == client.ClientName))
            {
                foreach (string message in entry.Value)
                {
                    client.SendMessage(message);
                }
            }
        }
    }

    private static string GetPrivateChatKey(string user1, string user2)
    {
        // Tạo một key duy nhất cho lịch sử chat riêng giữa hai người dùng (dùng để phân biệt cặp người dùng)
        return string.Compare(user1, user2) < 0 ? $"{user1}-{user2}" : $"{user2}-{user1}";
    }
}

class ClientHandler
{
    private TcpClient client;
    private NetworkStream stream;
    public string ClientName { get; private set; }

    public ClientHandler(TcpClient client)
    {
        this.client = client;
        this.stream = client.GetStream();
    }

    public void Run()
    {
        // Đọc tên client từ đầu
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        ClientName = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
        Console.WriteLine($"{ClientName} has joined the chat.");

        // Gửi lịch sử chat cho client mới
        ChatServer.SendChatHistory(this);

        // Thông báo cho mọi người về người dùng mới
        ChatServer.Broadcast($"{ClientName} has joined the chat.", ClientName);

        while (true)
        {
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                // Phân tích cú pháp lệnh
                if (message.StartsWith("/private"))
                {
                    // /private <tên người nhận> <tin nhắn>
                    string[] splitMessage = message.Split(' ', 3);
                    if (splitMessage.Length == 3)
                    {
                        string receiverName = splitMessage[1];
                        string privateMessage = splitMessage[2];
                        ChatServer.SendPrivateMessage(privateMessage, receiverName, ClientName);
                    }
                }
                else
                {
                    // Broadcast tin nhắn cho toàn bộ chat
                    ChatServer.Broadcast($"{ClientName}: {message}", ClientName);
                }
            }
            catch
            {
                break;
            }
        }

        Console.WriteLine($"{ClientName} has left the chat.");
        ChatServer.Broadcast($"{ClientName} has left the chat.", ClientName);
        client.Close();
    }

    public void SendMessage(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }
}
