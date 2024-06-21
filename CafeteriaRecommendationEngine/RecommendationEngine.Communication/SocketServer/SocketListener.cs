using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RecomendationEngine.Services.Interfaces;
using RecommendationEngine.DataModel.Models;

namespace RecommendationEngine.Communication.SocketServer
{
    public class SocketListener
    {
        private static ConcurrentDictionary<string, (Socket, User)> activeSessions = new ConcurrentDictionary<string, (Socket, User)>();

        public static async Task StartServer(IServiceProvider services)
        {
            var ipAddress = IPAddress.Parse("172.20.10.14");
            var localEndPoint = new IPEndPoint(ipAddress, 1111);

            var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    var handler = await listener.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var buffer = new byte[1024];
                            var bytesReceived = await handler.ReceiveAsync(buffer, SocketFlags.None);
                            var data = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

                            using (var scope = services.CreateScope())
                            {
                                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                                var response = await ProcessData(data, authService, handler, scope);
                                var msg = Encoding.ASCII.GetBytes(response);
                                await handler.SendAsync(msg, SocketFlags.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing request: {ex.Message}");
                        }
                        finally
                        {
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Server error: {e}");
            }
        }

        private static async Task<string> ProcessData(string data, IAuthService authService, Socket handler, IServiceScope scope)
        {
            try
            {
                Console.WriteLine($"Received data: {data}");
                var parts = data.Split(';');
                var command = parts[0];
                var username = parts.Length > 1 ? parts[1] : string.Empty;
                var password = parts.Length > 2 ? parts[2] : string.Empty;

                if (command == "login")
                {
                    if (activeSessions.ContainsKey(username))
                    {
                        return "Already logged in";
                    }

                    var user = await authService.Authenticate(username, password);
                    if (user == null)
                    {
                        return "Login failed";
                    }

                    activeSessions.TryAdd(username, (handler, user));

                    var response = $"Login successful; Role: {user.Role.RoleName}\n";
                    response += user.Role.RoleName.ToLower() switch
                    {
                        "admin" => "Options:\n1. Add Item\n2. Update Item\n3. Delete Item\n4. View Items\n5. Logout",
                        "chef" => "Options:\n1. Rollout Menu\n2. Logout",
                        "employee" => "Options:\n1. Give Feedback\n2. View Menu\n3. Logout",
                        _ => "Unknown role"
                    };

                    return response;
                }

                if (command == "logout")
                {
                    if (activeSessions.TryRemove(username, out _))
                    {
                        return "Logout successful";
                    }
                    else
                    {
                        return "Logout failed: User not logged in";
                    }
                }

                if (activeSessions.ContainsKey(username))
                {
                    var session = activeSessions[username];
                    var user = session.Item2;

                    Console.WriteLine($"Processing command '{command}' for user '{username}' with role '{user.Role.RoleName}'");

                    return user.Role.RoleName.ToLower() switch
                    {
                        "admin" => await HandleAdminCommands(parts.Skip(2).ToArray(), scope),
                        "chef" => await HandleChefCommands("", scope),
                        "employee" => await HandleEmployeeCommands("", scope),
                        _ => "Unknown role"
                    };
                }

                return "Unknown command";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing data: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static async Task<string> HandleAdminCommands(string[] commandParts, IServiceScope scope)
        {
            var command = commandParts[0];
            var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();

            try
            {
                switch (command)
                {
                    case "1":
                        Console.WriteLine("Enter item name:");
                        var itemName = Console.ReadLine();
                        Console.WriteLine("Enter price:");
                        var price = decimal.Parse(Console.ReadLine());
                        Console.WriteLine("Enter availability status:");
                        var status = Console.ReadLine();
                        await adminService.AddItem(itemName, price, status);
                        return "Item added successfully.";
                    case "2":
                        Console.WriteLine("Enter item ID:");
                        var itemId = int.Parse(Console.ReadLine());
                        Console.WriteLine("Enter item name:");
                        itemName = Console.ReadLine();
                        Console.WriteLine("Enter price:");
                        price = decimal.Parse(Console.ReadLine());
                        Console.WriteLine("Enter availability status:");
                        status = Console.ReadLine();
                        await adminService.UpdateItem(itemId, itemName, price, status);
                        return "Item updated successfully.";
                    case "3":
                        Console.WriteLine("Enter item ID:");
                        itemId = int.Parse(Console.ReadLine());
                        await adminService.DeleteItem(itemId);
                        return "Item deleted successfully.";
                    case "4":
                        var items = await adminService.GetItems();
                        var itemsList = string.Join("\n", items.Select(i => $"ID: {i.ItemId}, Name: {i.ItemName}, Price: {i.Price}, Status: {i.AvailabilityStatus}"));
                        return $"Items:\n{itemsList}";
                    default:
                        return "Invalid command for admin.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling admin command '{command}': {ex.Message}");
                return $"Error handling admin command '{command}': {ex.Message}";
            }
        }


        private static async Task<string> HandleChefCommands(string command, IServiceScope scope)
        {
            // Implement chef-specific commands
            switch (command)
            {
                case "1": // Rollout Menu
                    return "Menu rolled out successfully";
                case "2": // Logout
                    return "logout";
                default:
                    return "Unknown chef command";
            }
        }

        private static async Task<string> HandleEmployeeCommands(string command, IServiceScope scope)
        {
            // Implement employee-specific commands
            switch (command)
            {
                case "1": // Give Feedback
                    return "Feedback submitted successfully";
                case "2": // View Menu
                    return "Menu viewed successfully";
                case "3": // Logout
                    return "logout";
                default:
                    return "Unknown employee command";
            }
        }

    }
}