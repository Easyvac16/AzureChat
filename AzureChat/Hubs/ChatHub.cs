using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using AzureChat.Entity;
using System;
using AzureChat;

namespace SignalRChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatContext _context;
        private static readonly Dictionary<string, string> _users = new();
        private static readonly Dictionary<string, string> _connections = new Dictionary<string, string>();


        public ChatHub(ChatContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext.Request.Query["username"].ToString();
            Console.WriteLine("Username connected:" + username);

            if (!string.IsNullOrEmpty(username))
            {
                _connections[username] = Context.ConnectionId;
                _users[Context.ConnectionId] = username; 
            }

            await Clients.All.SendAsync("ReceiveUserList", _connections.Select(u => new { user = u }).ToList());
            await OpenGlobalChat();
            await base.OnConnectedAsync();
        }



        public async Task RegisterUser(string username, string password)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser == null)
            {
                var user = new User
                {
                    Username = username,
                    Password = password
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await Clients.All.SendAsync("UserConnected", username, user.Id);
                await GetUserList();
            }
            else
            {
                await Clients.Caller.SendAsync("UserAlreadyExists", "This username is already taken.");
            }
        }



        public async Task Login(string username, string password)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (existingUser != null)
            {
                await Clients.Caller.SendAsync("LoginSuccessful", existingUser.Id);
                if (!_users.ContainsKey(Context.ConnectionId))
                {
                    _users[Context.ConnectionId] = username;
                    _connections[username] = Context.ConnectionId; 
                    await Clients.All.SendAsync("UserConnected", username, existingUser.Id);
                    await GetUserList();
                }
            }

            else
            {
                
                await Clients.Caller.SendAsync("LoginFailed", "Invalid username or password.");
            }
        }


        public async Task SendPrivateMessage(string senderNickname, string receiverNickname, string message)
        {
            if (string.IsNullOrEmpty(senderNickname) || string.IsNullOrEmpty(receiverNickname) || string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("SenderNickname, ReceiverNickname, and Message cannot be null or empty.");
            }

            var senderUser = await GetUserByNickname(senderNickname);
            var receiverUser = await GetUserByNickname(receiverNickname);

            try
            {
                Console.WriteLine($"SenderId: {senderUser.Id}, ReceiverId: {receiverUser.Id}, Message: {message}");

                var chatMessage = new ChatMessage
                {
                    SenderId = senderUser.Id,
                    ReceiverId = receiverUser.Id,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                await _context.ChatMessages.AddAsync(chatMessage);
                await _context.SaveChangesAsync();

                if (_connections.TryGetValue(receiverUser.Username, out string? value))
                {
                    var receiverConnectionId = value;
                    await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", senderNickname, message);
                }
                else
                {
                    Console.WriteLine($"User {receiverNickname} is not connected.");
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database update error: {dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
        }


        private async Task<User> GetUserByNickname(string nickname)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == nickname);
            if (user == null)
            {
                Console.WriteLine($"User with nickname {nickname} not found.");
                throw new InvalidOperationException($"User with nickname {nickname} does not exist.");
            }
            return user;
        }



        public async Task OpenPrivateChat(string receiverUsername)
        {
            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == receiverUsername);

            if (!_users.TryGetValue(Context.ConnectionId, out var senderUsername))
            {
                await Clients.Caller.SendAsync("Error", "User not found in session.");
                return;
            }

            var sender = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == senderUsername);

            if (receiver != null && sender != null)
            {
                var messageHistory = await _context.ChatMessages
                    .Where(m => (m.SenderId == sender.Id && m.ReceiverId == receiver.Id)
                              || (m.SenderId == receiver.Id && m.ReceiverId == sender.Id))
                    .OrderBy(m => m.Timestamp) 
                    .ToListAsync();

                foreach (var message in messageHistory)
                {
                    var senderUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == message.SenderId);
                    await Clients.Caller.SendAsync("ReceivePrivateMessage", senderUser.Username, message.Message);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "User not found.");
            }
        }

        public async Task GetGlobalChatHistory()
        {
            var globalMessages = await _context.ChatMessages
                .Where(m => m.ReceiverId == null) 
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            foreach (var message in globalMessages)
            {
                var senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == message.SenderId);
                await Clients.Caller.SendAsync("ReceiveMessage", senderUser.Username, message.Message);
            }
        }


        public async Task SendMessage(string senderNickname, string message)
        {
            if (string.IsNullOrEmpty(senderNickname) || string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("SenderNickname and Message cannot be null or empty.");
            }

            var senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == senderNickname);

            if (senderUser == null)
            {
                throw new InvalidOperationException($"User with nickname {senderNickname} does not exist.");
            }

            try
            {
                Console.WriteLine($"SenderId: {senderUser.Id}, Message: {message}"); 

                var chatMessage = new ChatMessage
                {
                    SenderId = senderUser.Id,  
                    ReceiverId = null,          
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                await _context.ChatMessages.AddAsync(chatMessage);
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("ReceiveMessage", senderNickname, message);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                throw; 
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var username))
            {
                _connections.Remove(username);
                _users.Remove(Context.ConnectionId); 
            }

            await Clients.All.SendAsync("ReceiveUserList", _connections.Select(u => new { user = u }).ToList());
            await base.OnDisconnectedAsync(exception);
        }

        public async Task OpenGlobalChat()
        {
            await GetGlobalChatHistory();
        }

        public async Task GetUserList()
        {
            await Clients.Caller.SendAsync("ReceiveUserList", _connections.Select(u => new { user = u }).ToList());
        }
    }
}
