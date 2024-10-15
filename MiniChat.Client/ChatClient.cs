using System.Net.Sockets;
using System.Text;

class ChatClient
{
    private static int messageAreaHeight = 20; // Khu vực hiển thị tin nhắn
    private static int inputAreaHeight = 3;    // Khu vực nhập liệu
    private static string clientName;
    private static List<string> chatHistory = new List<string>(); // Lịch sử chat

    static void Main()
    {
        Console.Title = "Mini Chat Application";
        Console.Clear();
        DrawChatLayout();

        Console.SetCursorPosition(1, messageAreaHeight + 2);
        Console.Write("Enter your name: ");
        clientName = Console.ReadLine();

        TcpClient client = new TcpClient("127.0.0.1", 8888);
        NetworkStream stream = client.GetStream();

        // Gửi tên đến server
        byte[] nameData = Encoding.ASCII.GetBytes(clientName);
        stream.Write(nameData, 0, nameData.Length);

        // Tạo một thread để nhận tin nhắn từ server
        Thread receiveThread = new Thread(() => ReceiveMessages(stream));
        receiveThread.Start();

        while (true)
        {
            Console.SetCursorPosition(1, messageAreaHeight + 2);
            Console.Write(new string(' ', Console.WindowWidth - 2)); // Xóa dòng cũ
            Console.SetCursorPosition(1, messageAreaHeight + 2);
            Console.Write($"{clientName}: ");
            string message = Console.ReadLine();

            // Gửi tin nhắn tới server
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }

    static void DrawChatLayout()
    {
        // Kẻ đường viền và khu vực chat
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        Console.WriteLine(new string('=', Console.WindowWidth));

        for (int i = 1; i <= messageAreaHeight; i++)
        {
            Console.SetCursorPosition(0, i);
            Console.Write("|");
            Console.SetCursorPosition(Console.WindowWidth - 1, i);
            Console.Write("|");
        }

        Console.SetCursorPosition(0, messageAreaHeight + 1);
        Console.WriteLine(new string('=', Console.WindowWidth));

        // Khu vực nhập tin nhắn
        for (int i = messageAreaHeight + 2; i < messageAreaHeight + 2 + inputAreaHeight; i++)
        {
            Console.SetCursorPosition(0, i);
            Console.Write("|");
            Console.SetCursorPosition(Console.WindowWidth - 1, i);
            Console.Write("|");
        }

        Console.SetCursorPosition(0, messageAreaHeight + 2 + inputAreaHeight);
        Console.WriteLine(new string('=', Console.WindowWidth));
    }

    static void ReceiveMessages(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                AddMessageToHistory(message);
                RedrawChatHistory();
            }
            catch
            {
                break;
            }
        }
    }

    static void AddMessageToHistory(string message)
    {
        // Thêm tin nhắn vào lịch sử chat
        chatHistory.Add(message);
        if (chatHistory.Count > messageAreaHeight)
        {
            // Nếu vượt quá vùng hiển thị, xóa tin nhắn cũ nhất để cuộn lên
            chatHistory.RemoveAt(0);
        }
    }

    static void RedrawChatHistory()
    {
        // Lưu vị trí con trỏ hiện tại
        int currentCursorLeft = Console.CursorLeft;
        int currentCursorTop = Console.CursorTop;

        // Vẽ lại khu vực chat
        for (int i = 0; i < messageAreaHeight; i++)
        {
            Console.SetCursorPosition(1, i + 1);
            Console.Write(new string(' ', Console.WindowWidth - 2)); // Xóa dòng cũ
            if (i < chatHistory.Count)
            {
                Console.SetCursorPosition(1, i + 1);
                Console.Write(chatHistory[i]);
            }
        }

        // Khôi phục vị trí con trỏ
        Console.SetCursorPosition(currentCursorLeft, currentCursorTop);
    }
}
