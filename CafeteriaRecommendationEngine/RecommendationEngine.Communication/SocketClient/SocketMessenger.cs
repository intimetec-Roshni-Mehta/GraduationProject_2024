using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RecommendationEngine.Communication.SocketClient
{
    public class SocketMessenger
    {
        // Method to start the client socket
        public static void StartClient()
        {
            try
            {
                var remoteEP = new IPEndPoint(IPAddress.Parse("172.20.10.14"), 1111);

                using (Socket sender = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    ConnectToServer(sender, remoteEP);
                    SendLoginData(sender);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            // Keep the console window open
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        // Method to connect to the server
        private static void ConnectToServer(Socket sender, IPEndPoint remoteEP)
        {
            try
            {
                sender.Connect(remoteEP);
                Console.WriteLine(sender.RemoteEndPoint != null
                    ? $"Socket connected to {sender.RemoteEndPoint}"
                    : "Socket connected to the server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to server: {ex.Message}");
                throw;
            }
        }

        // Method to send login data to the server
        private static void SendLoginData(Socket sender)
        {
            try
            {
                string username = PromptUser("Enter username: ");
                string password = PromptUser("Enter password: ");
                string message = $"login;{username};{password}";

                SendMessage(sender, message);
                ReceiveResponse(sender, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send or receive data: {ex.Message}");
            }
        }

        // Method to prompt user for input
        private static string PromptUser(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }

        // Method to send message to the server
        private static void SendMessage(Socket sender, string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            sender.Send(msg);
        }

        // Method to receive and display server response
        private static void ReceiveResponse(Socket sender, string username)
        {
            try
            {
                byte[] bytes = new byte[1024];
                int bytesRec = sender.Receive(bytes);
                var response = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine("Server response = {0}", response);

                if (response.Contains("Options:"))
                {
                    bool loggedIn = true;
                    while (loggedIn)
                    {
                        Console.Write("Enter option (or 'logout' to exit): ");
                        var option = Console.ReadLine();

                        if (option.ToLower() == "logout" || option == "5")
                        {
                            SendMessage(sender, $"logout;{username};");
                            loggedIn = false;
                            break;
                        }

                        SendMessage(sender, $"{option};{username};");
                        ReceiveServerResponse(sender);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }
        }

        private static void ReceiveServerResponse(Socket sender)
        {
            try
            {
                byte[] bytes = new byte[1024];
                int bytesRec = sender.Receive(bytes);
                var response = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine("Server response = {0}", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }
        }

    }
}
