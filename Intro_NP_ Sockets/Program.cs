using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

class Program
{
    static void Main(string[] args)
    {
        InitializeDatabase();

        var serverThread = new System.Threading.Thread(StartServer);
        serverThread.Start();

        StartClient();
    }

    static void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            using (TcpClient client = listener.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string question = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string answer = GetAnswerFromDatabase(question);

                if (answer == null)
                {
                    Console.WriteLine("No answer found. Please provide a new one:");
                    string newAnswer = Console.ReadLine();
                    AddAnswerToDatabase(question, newAnswer);
                    answer = "Thank you for your answer!";
                }

                byte[] responseBytes = Encoding.UTF8.GetBytes(answer);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
    }

    static void StartClient()
    {
        while (true)
        {
            using (TcpClient client = new TcpClient("127.0.0.1", 5000))
            using (NetworkStream stream = client.GetStream())
            {
                Console.WriteLine("Enter your question:");
                string question = Console.ReadLine();
                byte[] questionBytes = Encoding.UTF8.GetBytes(question);
                stream.Write(questionBytes, 0, questionBytes.Length);

                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string answer = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Answer: " + answer);
            }

            Console.WriteLine("Do you want to ask another question? (yes/no)");
            string continueAsking = Console.ReadLine().ToLower();
            if (continueAsking != "yes")
            {
                break;
            }
        }
    }

    static void InitializeDatabase()
    {
        using (var connection = new SQLiteConnection("Data Source=responses.db;Version=3;"))
        {
            connection.Open();
            string createTableQuery = "CREATE TABLE IF NOT EXISTS Responses (Question TEXT PRIMARY KEY, Answer TEXT)";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            string[] defaultQuestions = {
                "How are you?",
                "What is your name?",
                "What is the weather like?",
                "Tell me a joke.",
                "What is the meaning of life?"
            };

            string[] defaultAnswers = {
                "I'm just a server, but thanks for asking!",
                "I am the Говорун server.",
                "I don't know, but you can check a weather website!",
                "Why don't scientists trust atoms? Because they make up everything!",
                "The meaning of life is 42."
            };

            for (int i = 0; i < defaultQuestions.Length; i++)
            {
                AddAnswerToDatabase(defaultQuestions[i], defaultAnswers[i], connection);
            }
        }
    }

    static string GetAnswerFromDatabase(string question)
    {
        using (var connection = new SQLiteConnection("Data Source=responses.db;Version=3;"))
        {
            connection.Open();
            string selectQuery = "SELECT Answer FROM Responses WHERE Question = @question";
            using (var command = new SQLiteCommand(selectQuery, connection))
            {
                command.Parameters.AddWithValue("@question", question);
                var result = command.ExecuteScalar();
                return result as string;
            }
        }
    }

    static void AddAnswerToDatabase(string question, string answer, SQLiteConnection connection = null)
    {
        bool externalConnection = connection != null;
        if (!externalConnection)
        {
            connection = new SQLiteConnection("Data Source=responses.db;Version=3;");
            connection.Open();
        }

        string insertQuery = "INSERT OR IGNORE INTO Responses (Question, Answer) VALUES (@question, @answer)";
        using (var command = new SQLiteCommand(insertQuery, connection))
        {
            command.Parameters.AddWithValue("@question", question);
            command.Parameters.AddWithValue("@answer", answer);
            command.ExecuteNonQuery();
        }

        if (!externalConnection)
        {
            connection.Close();
        }
    }
}