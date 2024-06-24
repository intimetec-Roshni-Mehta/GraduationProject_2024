using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RecommendationEngine.Communication.SocketClient
{
    public class SocketMessenger
    {
        public static void StartClient()
        {
            try
            {
                var remoteEP = new IPEndPoint(IPAddress.Parse("172.16.2.4"), 9999);

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

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private static void ConnectToServer(Socket sender, IPEndPoint remoteEP)
        {
            try
            {
                sender.Connect(remoteEP);
                Console.WriteLine($"Socket connected to {sender.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to server: {ex.Message}");
                throw;
            }
        }

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

        private static string PromptUser(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }

        private static void SendMessage(Socket sender, string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            sender.Send(msg);
        }

        private static void ReceiveResponse(Socket sender, string username)
        {
            try
            {
                byte[] bytes = new byte[1024];
                int bytesRec = sender.Receive(bytes);
                var response = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine("Server response = {0}", response);

                HandleRoleOptions(sender, response, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }
        }

        private static void HandleRoleOptions(Socket sender, string response, string username)
        {
            switch (response)
            {
                case "Login successful; Role: Admin":
                    Console.WriteLine("Options:\n1. Add Item\n2. Update Item\n3. Delete Item\n4. View Items\n");
                    ProcessAdminOptions(sender, username, "Admin");
                    break;
                case "Login successful; Role: Chef":
                    Console.WriteLine("Options:\n1. Rollout Menu\n");
                    ProcessChefOptions(sender, username);
                    break;
                case "Login successful; Role: Employee":
                    Console.WriteLine("Options:\n1. Give Feedback\n2. View Menu\n");
                    ProcessEmployeeOptions(sender, username);
                    break;
                default:
                    Console.WriteLine("Unknown role or invalid response");
                    break;
            }
        }

        private static void ProcessAdminOptions(Socket sender, string username, string option)
        {
            switch (option)
            {
                case "1":
                    string itemName = PromptUser("Enter item name: ");
                    string itemPrice = PromptUser("Enter item price: ");
                    string itemStatus = PromptUser("Enter item status: ");
                    string mealTypeId = PromptUser("Enter meal type ID: ");

                    SendMessage(sender, $"{option};{username};{itemName};{itemPrice};{itemStatus};{mealTypeId}");
                    break;
                case "2":
                    string itemIdToUpdate = PromptUser("Enter item id to update: ");
                    string newItemPrice = PromptUser("Enter new item price: ");
                    string newItemStatus = PromptUser("Enter new item status: ");

                    SendMessage(sender, $"{option};{username};{itemIdToUpdate};{newItemPrice};{newItemStatus}");
                    break;
                case "3":
                    string itemIdToDelete = PromptUser("Enter item ID to delete: ");

                    SendMessage(sender, $"{option};{username};{itemIdToDelete}");
                    break;
                case "4":
                    SendMessage(sender, $"{option};{username};");
                    break;
                default:
                    SendMessage(sender, $"{option};{username};");
                    break;
            }
        }

        private static void ProcessChefOptions(Socket sender, string username)
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.Write("Enter option (or 'logout' to exit): ");
                var option = Console.ReadLine();

                if (option.ToLower() == "logout")
                {
                    SendMessage(sender, $"logout;{username};");
                    loggedIn = false;
                }
                else
                {
                    SendMessage(sender, $"{option};{username};");
                    ReceiveServerResponse(sender);
                }
            }
        }

        private static void ProcessEmployeeOptions(Socket sender, string username)
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.Write("Enter option (or 'logout' to exit): ");
                var option = Console.ReadLine();

                if (option.ToLower() == "logout")
                {
                    SendMessage(sender, $"logout;{username};");
                    loggedIn = false;
                }
                else
                {
                    SendMessage(sender, $"{option};{username};");
                    ReceiveServerResponse(sender);
                }
            }
        }

        private static void ReceiveServerResponse(Socket sender)
        {
            try
            {
                byte[] bytes = new byte[2048];
                int bytesRec = sender.Receive(bytes);
                var response = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine("Server response =\n{0}", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }
        }
    }
}
