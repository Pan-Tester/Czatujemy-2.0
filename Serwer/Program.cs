using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CzatujiemyServer
{
    public class ChatMessage
    {
        public string Nick { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string MessageType { get; set; } // "message", "join", "leave", "channel_create", etc.
        public string ChannelId { get; set; }
        public string UserId { get; set; }
    }

    public class Client
    {
        public TcpClient TcpClient { get; set; }
        public NetworkStream Stream { get; set; }
        public string Nick { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; } // ID zalogowanego użytkownika
        public string CurrentChannelId { get; set; }
        public HashSet<string> JoinedChannels { get; set; } = new HashSet<string>();
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public bool IsAuthenticated { get; set; } = false; // Czy użytkownik jest zalogowany
    }



    class Program
    {
        private static TcpListener tcpListener;
        private static List<Client> clients = new List<Client>();
        private static Dictionary<string, Channel> channels = new Dictionary<string, Channel>();
        private static Dictionary<string, User> users = new Dictionary<string, User>(); // Nowy słownik użytkowników
        private static readonly string channelsFile = "channels.json";
        private static readonly string usersFile = "users.json"; // Nowy plik użytkowników
        private static readonly string globalChannelId = "global";

        static async Task Main(string[] args)
        {
            ShowStartupWarning();
            
            Console.WriteLine("=== Serwer Czatujemy z kanałami ===");
            Console.WriteLine("Uruchamianie serwera...");

            LoadChannels();
            LoadUsers();
            InitializeGlobalChannel();

            tcpListener = new TcpListener(IPAddress.Any, 8080);
            tcpListener.Start();
            
            Console.WriteLine("Serwer nasłuchuje na porcie 8080");
            Console.WriteLine($"Dostępne kanały: {channels.Count}");
            Console.WriteLine("Naciśnij Ctrl+C aby zatrzymać serwer");

            while (true)
            {
                try
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    Console.WriteLine($"Nowe połączenie: {tcpClient.Client.RemoteEndPoint}");
                    
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd: {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient tcpClient)
        {
            Client client = new Client
            {
                TcpClient = tcpClient,
                Stream = tcpClient.GetStream(),
                Id = Guid.NewGuid().ToString(),
                CurrentChannelId = globalChannelId
            };

            try
            {
                clients.Add(client);

                byte[] buffer = new byte[4096];
                
                while (true)
                {
                    int bytesRead = await client.Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string jsonData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    client.LastActivity = DateTime.Now;
                    
                    try
                    {
                        var data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                        string messageType = data.MessageType?.ToString();

                        await ProcessClientMessage(client, messageType, data);
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine("Otrzymano nieprawidłowe dane JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd obsługi klienta: {ex.Message}");
            }
            finally
            {
                await DisconnectClient(client);
            }
        }

        private static async Task ProcessClientMessage(Client client, string messageType, dynamic data)
        {
            switch (messageType)
            {
                case "login":
                    await HandleLogin(client, data);
                    break;
                case "register":
                    await HandleRegister(client, data);
                    break;
                case "change_nick":
                    await HandleChangeNick(client, data);
                    break;
                case "change_password":
                    await HandleChangePassword(client, data);
                    break;
                case "delete_channel":
                    await HandleDeleteChannel(client, data);
                    break;
                case "get_owned_channels":
                    await HandleGetOwnedChannels(client);
                    break;
                case "nick":
                    await HandleNickChange(client, data);
                    break;
                case "message":
                    await HandleChatMessage(client, data);
                    break;
                case "join_channel":
                    await HandleJoinChannel(client, data);
                    break;
                case "create_channel":
                    await HandleCreateChannel(client, data);
                    break;
                case "leave_channel":
                    await HandleLeaveChannel(client, data);
                    break;
                case "get_channels":
                    await HandleGetChannels(client);
                    break;
                case "get_channel_members":
                    await HandleGetChannelMembers(client, data);
                    break;
                case "kick_user":
                    await HandleKickUser(client, data);
                    break;
                case "ban_user":
                    await HandleBanUser(client, data);
                    break;
                case "unban_user":
                    await HandleUnbanUser(client, data);
                    break;
                case "mute_user":
                    await HandleMuteUser(client, data);
                    break;
                case "unmute_user":
                    await HandleUnmuteUser(client, data);
                    break;
                case "update_channel":
                    await HandleUpdateChannel(client, data);
                    break;
                case "get_online_users":
                    await HandleGetOnlineUsers(client);
                    break;
                case "typing":
                    await HandleTypingStatus(client, data);
                    break;
                case "get_channel_history":
                    await HandleGetChannelHistory(client, data);
                    break;
                case "switch_channel":
                    await HandleSwitchChannel(client, data);
                    break;
                default:
                    Console.WriteLine($"Nieznany typ wiadomości: {messageType}");
                    break;
            }
        }

        private static async Task HandleNickChange(Client client, dynamic data)
        {
            string oldNick = client.Nick;
            client.Nick = data.Nick?.ToString();
            Console.WriteLine($"Klient ustawił nick: {client.Nick}");

            // Dodaj do kanału globalnego
            if (channels.ContainsKey(globalChannelId))
            {
                channels[globalChannelId].AddMember(client.Id);
                client.JoinedChannels.Add(globalChannelId);
            }

            // Wyślij listę kanałów i historię
            await SendChannelListToClient(client);
            await SendChannelHistory(client, client.CurrentChannelId);

            // Powiadom kanał o dołączeniu
            if (string.IsNullOrEmpty(oldNick))
            {
                var joinMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{client.Nick} dołączył do czatu",
                    Timestamp = DateTime.Now,
                    MessageType = "join",
                    ChannelId = client.CurrentChannelId,
                    UserId = "system"
                };

                await BroadcastToChannel(client.CurrentChannelId, joinMessage, null);
                
                // Rozgłoś aktualizację listy użytkowników online
                await BroadcastOnlineUsers();
            }
        }

        private static async Task HandleChatMessage(Client client, dynamic data)
        {
            string channelId = client.CurrentChannelId;
            Console.WriteLine($"[DEBUG] HandleChatMessage: client={client.Nick}, channelId={channelId}, message={data.Message}");
            
            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            var channel = channels[channelId];
            
            if (!channel.CanUserSpeak(client.Id))
            {
                await SendErrorToClient(client, "Nie możesz pisać na tym kanale");
                return;
            }

            var message = new ChatMessage
            {
                Nick = client.Nick ?? "Nieznany",
                Message = data.Message?.ToString(),
                Timestamp = DateTime.Now,
                MessageType = "message",
                ChannelId = channelId,
                UserId = client.Id
            };

            Console.WriteLine($"[{channel.Name}] {message.Nick}: {message.Message}");

            // Dodaj do historii kanału
            channel.Messages.Add(message);
            SaveChannels();

            // Wyślij do wszystkich klientów na kanale
            await BroadcastToChannel(channelId, message, null);
        }

        private static async Task HandleJoinChannel(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string password = data.Password?.ToString();

            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            var channel = channels[channelId];

            if (!channel.CanUserJoin(client.Id, password))
            {
                await SendErrorToClient(client, "Nie możesz dołączyć do tego kanału");
                return;
            }

            if (client.JoinedChannels.Count >= 10)
            {
                await SendErrorToClient(client, "Możesz należeć maksymalnie do 10 kanałów");
                return;
            }

            channel.AddMember(client.Id);
            client.JoinedChannels.Add(channelId);
            client.CurrentChannelId = channelId;

            // Powiadom o dołączeniu
            var joinMessage = new ChatMessage
            {
                Nick = "System",
                Message = $"{client.Nick} dołączył do kanału",
                Timestamp = DateTime.Now,
                MessageType = "join",
                ChannelId = channelId,
                UserId = "system"
            };

            await BroadcastToChannel(channelId, joinMessage, client);
            await SendChannelListToClient(client);
            await SendChannelHistory(client, channelId);

            Console.WriteLine($"{client.Nick} dołączył do kanału {channel.Name}");
        }

        private static async Task HandleCreateChannel(Client client, dynamic data)
        {
            string name = data.Name?.ToString();
            string icon = data.Icon?.ToString();
            string password = data.Password?.ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                await SendErrorToClient(client, "Nazwa kanału nie może być pusta");
                return;
            }

            // Sprawdź czy użytkownik nie ma zbyt wielu kanałów
            int userChannelsCount = channels.Values.Count(c => c.OwnerId == client.Id);
            if (userChannelsCount >= 10)
            {
                await SendErrorToClient(client, "Możesz utworzyć maksymalnie 10 kanałów");
                return;
            }

            var channel = new Channel
            {
                Name = name,
                Icon = icon ?? "💬",
                Password = password,
                OwnerId = client.Id,
                IsGlobal = false
            };

            channels[channel.Id] = channel;
            channel.AddMember(client.Id);
            channel.MemberRoles[client.Id] = ChannelRole.Owner;
            
            client.JoinedChannels.Add(channel.Id);
            client.CurrentChannelId = channel.Id;

            // Dodaj kanał do listy własnych kanałów użytkownika
            if (client.IsAuthenticated && users.ContainsKey(client.UserId))
            {
                users[client.UserId].OwnedChannels.Add(channel.Id);
                SaveUsers();
            }

            SaveChannels();

            await SendChannelListToClient(client);
            await SendSuccessToClient(client, $"Kanał '{name}' został utworzony");

            Console.WriteLine($"{client.Nick} utworzył kanał {name}");
        }

        private static async Task HandleLeaveChannel(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();

            if (channelId == globalChannelId)
            {
                await SendErrorToClient(client, "Nie możesz opuścić kanału globalnego");
                return;
            }

            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            var channel = channels[channelId];
            channel.RemoveMember(client.Id);
            client.JoinedChannels.Remove(channelId);

            if (client.CurrentChannelId == channelId)
            {
                client.CurrentChannelId = globalChannelId;
            }

            // Powiadom o opuszczeniu
            var leaveMessage = new ChatMessage
            {
                Nick = "System",
                Message = $"{client.Nick} opuścił kanał",
                Timestamp = DateTime.Now,
                MessageType = "leave",
                ChannelId = channelId,
                UserId = "system"
            };

            await BroadcastToChannel(channelId, leaveMessage, null);
            await SendChannelListToClient(client);
            await SendChannelHistory(client, client.CurrentChannelId);

            Console.WriteLine($"{client.Nick} opuścił kanał {channel.Name}");
        }

        private static async Task SendChannelHistory(Client client, string channelId)
        {
            if (!channels.ContainsKey(channelId)) return;

            var channel = channels[channelId];
            var historyData = new
            {
                MessageType = "history",
                ChannelId = channelId,
                Messages = channel.Messages.TakeLast(50)
            };
            
            await SendToClient(client, historyData);
        }

        private static async Task HandleGetChannelHistory(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            
            if (string.IsNullOrEmpty(channelId))
            {
                await SendErrorToClient(client, "Nie podano ID kanału");
                return;
            }

            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            // Sprawdź czy użytkownik ma dostęp do kanału
            var channel = channels[channelId];
            if (!channel.IsGlobal && !client.JoinedChannels.Contains(channelId))
            {
                await SendErrorToClient(client, "Nie masz dostępu do tego kanału");
                return;
            }

            await SendChannelHistory(client, channelId);
        }

        private static async Task HandleSwitchChannel(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            Console.WriteLine($"[DEBUG] HandleSwitchChannel: client={client.Nick}, channelId={channelId}, currentChannelId={client.CurrentChannelId}");
            
            if (string.IsNullOrEmpty(channelId))
            {
                await SendErrorToClient(client, "Nie podano ID kanału");
                return;
            }

            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            var channel = channels[channelId];
            
            // Sprawdź czy użytkownik ma dostęp do kanału
            if (!channel.IsGlobal && !client.JoinedChannels.Contains(channelId))
            {
                await SendErrorToClient(client, "Nie masz dostępu do tego kanału");
                return;
            }

            // Zmień aktualny kanał klienta
            string oldChannelId = client.CurrentChannelId;
            client.CurrentChannelId = channelId;
            Console.WriteLine($"[DEBUG] Serwer: currentChannelId zmienione z {oldChannelId} na {client.CurrentChannelId} dla klienta {client.Nick}");
            
            // Wyślij potwierdzenie przełączenia kanału
            var switchResponse = new
            {
                MessageType = "channel_switched",
                ChannelId = channelId,
                ChannelName = channel.Name,
                ChannelIcon = channel.Icon
            };
            await SendToClient(client, switchResponse);
            
            // Wyślij historię nowego kanału
            await SendChannelHistory(client, channelId);
            
            // Wyślij zaktualizowaną listę kanałów
            await SendChannelListToClient(client);
            
            Console.WriteLine($"{client.Nick} przełączył się na kanał {channel.Name}");
        }

        private static async Task SendChannelListToClient(Client client)
        {
            var channelList = channels.Values
                .Where(c => c.IsGlobal || client.JoinedChannels.Contains(c.Id))
                .Select(c => new ChannelInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    HasPassword = !string.IsNullOrEmpty(c.Password),
                    MemberCount = c.ActiveMembers.Count,
                    IsGlobal = c.IsGlobal,
                    OwnerId = c.OwnerId
                }).ToList();

            var data = new
            {
                MessageType = "channel_list",
                Channels = channelList,
                CurrentChannelId = client.CurrentChannelId
            };

            await SendToClient(client, data);
        }

        private static async Task HandleGetChannels(Client client)
        {
            // Wyślij listę wszystkich dostępnych kanałów (do dołączenia)
            var availableChannels = channels.Values
                .Where(c => !c.IsGlobal && !client.JoinedChannels.Contains(c.Id))
                .Select(c => new ChannelInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    HasPassword = !string.IsNullOrEmpty(c.Password),
                    MemberCount = c.ActiveMembers.Count,
                    IsGlobal = false,
                    OwnerId = c.OwnerId
                }).ToList();

            var data = new
            {
                MessageType = "available_channels",
                Channels = availableChannels
            };

            await SendToClient(client, data);
        }

        private static async Task HandleGetChannelMembers(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            
            if (!channels.ContainsKey(channelId))
            {
                await SendErrorToClient(client, "Kanał nie istnieje");
                return;
            }

            var channel = channels[channelId];
            var members = new List<ChannelMember>();

            foreach (var memberId in channel.AllTimeMembers)
            {
                var memberClient = clients.FirstOrDefault(c => c.Id == memberId);
                var member = new ChannelMember
                {
                    UserId = memberId,
                    Nick = memberClient?.Nick ?? "Nieznany",
                    Role = channel.GetUserRole(memberId),
                    IsActive = channel.ActiveMembers.Contains(memberId),
                    IsBanned = channel.BannedUsers.Contains(memberId),
                    IsMuted = channel.MutedUsers.Contains(memberId),
                    LastSeen = memberClient?.LastActivity ?? DateTime.MinValue
                };
                members.Add(member);
            }

            var response = new
            {
                MessageType = "channel_members",
                ChannelId = channelId,
                Members = members,
                CanManage = channel.CanUserManage(client.Id)
            };

            await SendToClient(client, response);
        }

        private static async Task HandleKickUser(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string targetUserId = data.UserId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserModerate(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień");
                return;
            }

            var channel = channels[channelId];
            var targetClient = clients.FirstOrDefault(c => c.Id == targetUserId);
            
            if (targetClient != null)
            {
                channel.RemoveMember(targetUserId);
                targetClient.JoinedChannels.Remove(channelId);
                
                if (targetClient.CurrentChannelId == channelId)
                {
                    targetClient.CurrentChannelId = globalChannelId;
                    await SendChannelHistory(targetClient, globalChannelId);
                }

                var kickMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{targetClient.Nick} został wykopany przez {client.Nick}",
                    Timestamp = DateTime.Now,
                    MessageType = "kick",
                    ChannelId = channelId,
                    UserId = "system"
                };

                await BroadcastToChannel(channelId, kickMessage, null);
                await SendErrorToClient(targetClient, "Zostałeś wykopany z kanału");
            }
        }

        private static async Task HandleBanUser(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string targetUserId = data.UserId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserModerate(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień");
                return;
            }

            var channel = channels[channelId];
            var targetClient = clients.FirstOrDefault(c => c.Id == targetUserId);
            
            channel.BanUser(targetUserId);
            
            if (targetClient != null)
            {
                targetClient.JoinedChannels.Remove(channelId);
                
                if (targetClient.CurrentChannelId == channelId)
                {
                    targetClient.CurrentChannelId = globalChannelId;
                    await SendChannelHistory(targetClient, globalChannelId);
                }

                var banMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{targetClient.Nick} został zbanowany przez {client.Nick}",
                    Timestamp = DateTime.Now,
                    MessageType = "ban",
                    ChannelId = channelId,
                    UserId = "system"
                };

                await BroadcastToChannel(channelId, banMessage, null);
                await SendErrorToClient(targetClient, "Zostałeś zbanowany na tym kanale");
            }

            SaveChannels();
        }

        private static async Task HandleUnbanUser(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string targetUserId = data.UserId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserModerate(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień");
                return;
            }

            var channel = channels[channelId];
            channel.UnbanUser(targetUserId);
            
            var targetClient = clients.FirstOrDefault(c => c.Id == targetUserId);
            string targetNick = targetClient?.Nick ?? "Nieznany";

            var unbanMessage = new ChatMessage
            {
                Nick = "System",
                Message = $"{targetNick} został odbanowany przez {client.Nick}",
                Timestamp = DateTime.Now,
                MessageType = "unban",
                ChannelId = channelId,
                UserId = "system"
            };

            await BroadcastToChannel(channelId, unbanMessage, null);
            SaveChannels();
        }

        private static async Task HandleMuteUser(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string targetUserId = data.UserId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserModerate(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień");
                return;
            }

            var channel = channels[channelId];
            channel.MuteUser(targetUserId);
            
            var targetClient = clients.FirstOrDefault(c => c.Id == targetUserId);
            string targetNick = targetClient?.Nick ?? "Nieznany";

            var muteMessage = new ChatMessage
            {
                Nick = "System",
                Message = $"{targetNick} został wyciszony przez {client.Nick}",
                Timestamp = DateTime.Now,
                MessageType = "mute",
                ChannelId = channelId,
                UserId = "system"
            };

            await BroadcastToChannel(channelId, muteMessage, null);
            if (targetClient != null)
            {
                await SendErrorToClient(targetClient, "Zostałeś wyciszony na tym kanale");
            }
            
            SaveChannels();
        }

        private static async Task HandleUnmuteUser(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            string targetUserId = data.UserId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserModerate(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień");
                return;
            }

            var channel = channels[channelId];
            channel.UnmuteUser(targetUserId);
            
            var targetClient = clients.FirstOrDefault(c => c.Id == targetUserId);
            string targetNick = targetClient?.Nick ?? "Nieznany";

            var unmuteMessage = new ChatMessage
            {
                Nick = "System",
                Message = $"{targetNick} może ponownie pisać (przez {client.Nick})",
                Timestamp = DateTime.Now,
                MessageType = "unmute",
                ChannelId = channelId,
                UserId = "system"
            };

            await BroadcastToChannel(channelId, unmuteMessage, null);
            SaveChannels();
        }

        private static async Task HandleUpdateChannel(Client client, dynamic data)
        {
            string channelId = data.ChannelId?.ToString();
            
            if (!channels.ContainsKey(channelId) || !channels[channelId].CanUserManage(client.Id))
            {
                await SendErrorToClient(client, "Brak uprawnień do edycji kanału");
                return;
            }

            var channel = channels[channelId];
            
            if (data.Name != null) channel.Name = data.Name.ToString();
            if (data.Icon != null) channel.Icon = data.Icon.ToString();
            if (data.Password != null) channel.Password = data.Password.ToString();

            SaveChannels();

            // Powiadom wszystkich o zmianie
            foreach (var memberId in channel.ActiveMembers)
            {
                var memberClient = clients.FirstOrDefault(c => c.Id == memberId);
                if (memberClient != null)
                {
                    await SendChannelListToClient(memberClient);
                }
            }

            await SendSuccessToClient(client, "Kanał został zaktualizowany");
        }

        private static async Task HandleGetOnlineUsers(Client client)
        {
            var onlineUsers = clients.Where(c => !string.IsNullOrEmpty(c.Nick))
                                   .Select(c => c.Nick)
                                   .ToList();

            var onlineUsersData = new
            {
                MessageType = "online_users",
                Users = onlineUsers
            };

            await SendToClient(client, onlineUsersData);
        }

        private static async Task BroadcastOnlineUsers()
        {
            var onlineUsers = clients.Where(c => !string.IsNullOrEmpty(c.Nick))
                                   .Select(c => c.Nick)
                                   .ToList();

            var onlineUsersData = new
            {
                MessageType = "online_users",
                Users = onlineUsers
            };

            foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.Nick)).ToList())
            {
                try
                {
                    await SendToClient(client, onlineUsersData);
                }
                catch
                {
                    // Ignoruj błędy wysyłania
                }
            }
        }

        private static async Task HandleTypingStatus(Client client, dynamic data)
        {
            try
            {
                bool isTyping = data.IsTyping == true;
                string channelId = data.ChannelId?.ToString();

                if (string.IsNullOrEmpty(channelId)) return;

                var typingData = new
                {
                    MessageType = "typing",
                    User = client.Nick,
                    IsTyping = isTyping,
                    ChannelId = channelId
                };

                // Wyślij do wszystkich innych użytkowników na tym kanale
                if (channels.ContainsKey(channelId))
                {
                    foreach (var memberId in channels[channelId].ActiveMembers)
                    {
                        var member = clients.FirstOrDefault(c => c.Id == memberId && c != client);
                        if (member != null)
                        {
                            try
                            {
                                await SendToClient(member, typingData);
                            }
                            catch
                            {
                                // Ignoruj błędy wysyłania
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd obsługi typing status: {ex.Message}");
            }
        }

        private static async Task BroadcastToChannel(string channelId, ChatMessage message, Client excludeClient)
        {
            if (!channels.ContainsKey(channelId)) return;
            
            var channel = channels[channelId];
            List<Client> clientsToRemove = new List<Client>();

            foreach (var memberId in channel.ActiveMembers)
            {
                var client = clients.FirstOrDefault(c => c.Id == memberId);
                if (client == null || client == excludeClient) continue;

                try
                {
                    await SendToClient(client, message);
                }
                catch
                {
                    clientsToRemove.Add(client);
                }
            }

            // Usuń rozłączonych klientów
            foreach (var client in clientsToRemove)
            {
                await DisconnectClient(client);
            }
        }

        private static async Task SendToClient(Client client, object data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await client.Stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wysyłania do klienta {client.Nick}: {ex.Message}");
                throw;
            }
        }

        private static async Task SendErrorToClient(Client client, string error)
        {
            var errorData = new
            {
                MessageType = "error",
                Message = error
            };
            
            try
            {
                await SendToClient(client, errorData);
            }
            catch
            {
                // Ignoruj błędy wysyłania błędów
            }
        }

        private static async Task SendSuccessToClient(Client client, string message)
        {
            var successData = new
            {
                MessageType = "success",
                Message = message
            };
            
            try
            {
                await SendToClient(client, successData);
            }
            catch
            {
                // Ignoruj błędy
            }
        }

        private static void UpdateChannelOwnership(string userId, string newClientId)
        {
            // Znajdź wszystkie kanały należące do użytkownika i zaktualizuj OwnerId
            foreach (var channel in channels.Values.Where(c => users.ContainsKey(userId) && users[userId].OwnedChannels.Contains(c.Id)))
            {
                channel.OwnerId = newClientId;
                // Upewnij się, że rola właściciela jest prawidłowo ustawiona
                channel.MemberRoles[newClientId] = ChannelRole.Owner;
            }
            SaveChannels();
        }

        private static async Task DisconnectClient(Client client)
        {
            clients.Remove(client);

            // Usuń ze wszystkich kanałów
            foreach (var channelId in client.JoinedChannels.ToList())
            {
                if (channels.ContainsKey(channelId))
                {
                    var channel = channels[channelId];
                    channel.RemoveMember(client.Id);

                    if (!string.IsNullOrEmpty(client.Nick))
                    {
                        var leaveMessage = new ChatMessage
                        {
                            Nick = "System",
                            Message = $"{client.Nick} opuścił czat",
                            Timestamp = DateTime.Now,
                            MessageType = "leave",
                            ChannelId = channelId,
                            UserId = "system"
                        };

                        await BroadcastToChannel(channelId, leaveMessage, null);
                    }
                }
            }

            if (!string.IsNullOrEmpty(client.Nick))
            {
                Console.WriteLine($"Klient {client.Nick} rozłączył się");
                
                // Rozgłoś aktualizację listy użytkowników online
                await BroadcastOnlineUsers();
            }

            client.TcpClient?.Close();
        }

        private static void InitializeGlobalChannel()
        {
            if (!channels.ContainsKey(globalChannelId))
            {
                var globalChannel = new Channel
                {
                    Id = globalChannelId,
                    Name = "🌍 Globalny",
                    Icon = "🌍",
                    IsGlobal = true,
                    OwnerId = "system"
                };
                
                channels[globalChannelId] = globalChannel;
                Console.WriteLine("Utworzono kanał globalny");
            }
        }

        private static void LoadChannels()
        {
            try
            {
                if (File.Exists(channelsFile))
                {
                    string json = File.ReadAllText(channelsFile, Encoding.UTF8);
                    var loadedChannels = JsonConvert.DeserializeObject<Dictionary<string, Channel>>(json);
                    
                    if (loadedChannels != null)
                    {
                        channels = loadedChannels;
                        
                        // Wyczyść aktywnych członków (po restarcie wszyscy są nieaktywni)
                        foreach (var channel in channels.Values)
                        {
                            channel.ActiveMembers.Clear();
                        }
                        
                        Console.WriteLine($"Wczytano {channels.Count} kanałów z pliku");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wczytywania kanałów: {ex.Message}");
                channels = new Dictionary<string, Channel>();
            }
        }

        private static void SaveChannels()
        {
            try
            {
                string json = JsonConvert.SerializeObject(channels, Formatting.Indented);
                File.WriteAllText(channelsFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd zapisywania kanałów: {ex.Message}");
            }
        }

        // === NOWE FUNKCJE DLA SYSTEMU UŻYTKOWNIKÓW ===

        private static void LoadUsers()
        {
            try
            {
                if (File.Exists(usersFile))
                {
                    string json = File.ReadAllText(usersFile, Encoding.UTF8);
                    var loadedUsers = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);
                    
                    if (loadedUsers != null)
                    {
                        users = loadedUsers;
                        
                        // Oznacz wszystkich jako offline po restarcie
                        foreach (var user in users.Values)
                        {
                            user.IsOnline = false;
                            user.CurrentClientId = null;
                        }
                        
                        Console.WriteLine($"Wczytano {users.Count} użytkowników z pliku");
                    }
                }
                else
                {
                    users = new Dictionary<string, User>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wczytywania użytkowników: {ex.Message}");
                users = new Dictionary<string, User>();
            }
        }

        private static void SaveUsers()
        {
            try
            {
                string json = JsonConvert.SerializeObject(users, Formatting.Indented);
                File.WriteAllText(usersFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd zapisywania użytkowników: {ex.Message}");
            }
        }

        private static async Task HandleLogin(Client client, dynamic data)
        {
            try
            {
                string nick = data.Nick?.ToString();
                string password = data.Password?.ToString();

                if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(password))
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Nick i hasło są wymagane"
                    };
                    await SendToClient(client, new { MessageType = "login_response", Response = errorResponse });
                    return;
                }

                var user = users.Values.FirstOrDefault(u => u.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Użytkownik nie istnieje"
                    };
                    await SendToClient(client, new { MessageType = "login_response", Response = errorResponse });
                    return;
                }

                if (!user.VerifyPassword(password))
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Nieprawidłowe hasło"
                    };
                    await SendToClient(client, new { MessageType = "login_response", Response = errorResponse });
                    return;
                }

                // Jeśli użytkownik jest już zalogowany, rozłącz poprzednie połączenie
                if (user.IsOnline)
                {
                    var existingClient = clients.FirstOrDefault(c => c.UserId == user.Id);
                    if (existingClient != null)
                    {
                        await DisconnectClient(existingClient);
                    }
                }

                // Zaloguj użytkownika
                client.IsAuthenticated = true;
                client.UserId = user.Id;
                client.Nick = user.Nick;
                user.IsOnline = true;
                user.CurrentClientId = client.Id;
                user.UpdateLastLogin();

                // Zaktualizuj właściciela kanałów użytkownika na nowy client.Id
                UpdateChannelOwnership(user.Id, client.Id);

                // Dodaj użytkownika do własnych kanałów
                foreach (var channelId in user.OwnedChannels.ToList())
                {
                    if (channels.ContainsKey(channelId))
                    {
                        var channel = channels[channelId];
                        channel.AddMember(client.Id);
                        channel.MemberRoles[client.Id] = ChannelRole.Owner;
                        client.JoinedChannels.Add(channelId);
                    }
                }

                // Dodaj do kanału globalnego
                if (channels.ContainsKey(globalChannelId))
                {
                    channels[globalChannelId].AddMember(client.Id);
                    client.JoinedChannels.Add(globalChannelId);
                }

                SaveUsers();

                var successResponse = new LoginResponse
                {
                    Success = true,
                    Message = "Pomyślnie zalogowano",
                    UserId = user.Id,
                    OwnedChannels = user.OwnedChannels
                };

                await SendToClient(client, new { MessageType = "login_response", Response = successResponse });
                
                // Wyślij listę kanałów i historię
                await SendChannelListToClient(client);
                await SendChannelHistory(client, client.CurrentChannelId);

                // Powiadom kanał o dołączeniu
                var joinMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{client.Nick} dołączył do czatu",
                    Timestamp = DateTime.Now,
                    MessageType = "join",
                    ChannelId = client.CurrentChannelId,
                    UserId = "system"
                };

                await BroadcastToChannel(client.CurrentChannelId, joinMessage, null);
                await BroadcastOnlineUsers();

                Console.WriteLine($"Użytkownik {nick} zalogował się");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd logowania: {ex.Message}");
                var errorResponse = new LoginResponse
                {
                    Success = false,
                    Message = "Błąd serwera podczas logowania"
                };
                await SendToClient(client, new { MessageType = "login_response", Response = errorResponse });
            }
        }

        private static async Task HandleRegister(Client client, dynamic data)
        {
            try
            {
                string nick = data.Nick?.ToString();
                string password = data.Password?.ToString();

                if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(password))
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Nick i hasło są wymagane"
                    };
                    await SendToClient(client, new { MessageType = "register_response", Response = errorResponse });
                    return;
                }

                if (nick.Length < 3 || password.Length < 6)
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Nick musi mieć min. 3 znaki, hasło min. 6 znaków"
                    };
                    await SendToClient(client, new { MessageType = "register_response", Response = errorResponse });
                    return;
                }

                // Sprawdź czy nick już istnieje
                if (users.Values.Any(u => u.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase)))
                {
                    var errorResponse = new LoginResponse
                    {
                        Success = false,
                        Message = "Użytkownik o tym nicku już istnieje"
                    };
                    await SendToClient(client, new { MessageType = "register_response", Response = errorResponse });
                    return;
                }

                // Utwórz nowego użytkownika
                var newUser = new User
                {
                    Nick = nick,
                    PasswordHash = User.HashPassword(password),
                    IsOnline = true,
                    CurrentClientId = client.Id
                };

                users[newUser.Id] = newUser;

                // Ustaw klienta jako zalogowanego
                client.IsAuthenticated = true;
                client.UserId = newUser.Id;
                client.Nick = newUser.Nick;

                // Dodaj do kanału globalnego
                if (channels.ContainsKey(globalChannelId))
                {
                    channels[globalChannelId].AddMember(client.Id);
                    client.JoinedChannels.Add(globalChannelId);
                }

                SaveUsers();

                var successResponse = new LoginResponse
                {
                    Success = true,
                    Message = "Konto zostało utworzone i zalogowano",
                    UserId = newUser.Id,
                    OwnedChannels = newUser.OwnedChannels
                };

                await SendToClient(client, new { MessageType = "register_response", Response = successResponse });
                
                // Wyślij listę kanałów i historię
                await SendChannelListToClient(client);
                await SendChannelHistory(client, client.CurrentChannelId);

                // Powiadom kanał o dołączeniu
                var joinMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{client.Nick} dołączył do czatu (nowy użytkownik)",
                    Timestamp = DateTime.Now,
                    MessageType = "join",
                    ChannelId = client.CurrentChannelId,
                    UserId = "system"
                };

                await BroadcastToChannel(client.CurrentChannelId, joinMessage, null);
                await BroadcastOnlineUsers();

                Console.WriteLine($"Zarejestrowano nowego użytkownika: {nick}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd rejestracji: {ex.Message}");
                var errorResponse = new LoginResponse
                {
                    Success = false,
                    Message = "Błąd serwera podczas rejestracji"
                };
                await SendToClient(client, new { MessageType = "register_response", Response = errorResponse });
            }
        }

        private static async Task HandleChangeNick(Client client, dynamic data)
        {
            if (!client.IsAuthenticated)
            {
                await SendErrorToClient(client, "Musisz być zalogowany");
                return;
            }

            try
            {
                string newNick = data.NewNick?.ToString();

                if (string.IsNullOrWhiteSpace(newNick) || newNick.Length < 3)
                {
                    await SendErrorToClient(client, "Nick musi mieć co najmniej 3 znaki");
                    return;
                }

                // Sprawdź czy nick już istnieje
                if (users.Values.Any(u => u.Nick.Equals(newNick, StringComparison.OrdinalIgnoreCase) && u.Id != client.UserId))
                {
                    await SendErrorToClient(client, "Użytkownik o tym nicku już istnieje");
                    return;
                }

                var user = users[client.UserId];
                string oldNick = user.Nick;
                user.Nick = newNick;
                client.Nick = newNick;

                SaveUsers();

                await SendSuccessToClient(client, "Nick został zmieniony");

                // Powiadom wszystkie kanały o zmianie nicku
                var changeMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"{oldNick} zmienił nick na {newNick}",
                    Timestamp = DateTime.Now,
                    MessageType = "nick_change",
                    UserId = "system"
                };

                foreach (var channelId in client.JoinedChannels)
                {
                    await BroadcastToChannel(channelId, changeMessage, null);
                }

                await BroadcastOnlineUsers();

                Console.WriteLine($"Użytkownik {oldNick} zmienił nick na {newNick}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd zmiany nicku: {ex.Message}");
                await SendErrorToClient(client, "Błąd serwera podczas zmiany nicku");
            }
        }

        private static async Task HandleChangePassword(Client client, dynamic data)
        {
            if (!client.IsAuthenticated)
            {
                await SendErrorToClient(client, "Musisz być zalogowany");
                return;
            }

            try
            {
                string currentPassword = data.CurrentPassword?.ToString();
                string newPassword = data.NewPassword?.ToString();

                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    await SendErrorToClient(client, "Obecne i nowe hasło są wymagane");
                    return;
                }

                if (newPassword.Length < 6)
                {
                    await SendErrorToClient(client, "Nowe hasło musi mieć co najmniej 6 znaków");
                    return;
                }

                var user = users[client.UserId];

                if (!user.VerifyPassword(currentPassword))
                {
                    await SendErrorToClient(client, "Nieprawidłowe obecne hasło");
                    return;
                }

                user.PasswordHash = User.HashPassword(newPassword);
                SaveUsers();

                await SendSuccessToClient(client, "Hasło zostało zmienione");

                Console.WriteLine($"Użytkownik {client.Nick} zmienił hasło");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd zmiany hasła: {ex.Message}");
                await SendErrorToClient(client, "Błąd serwera podczas zmiany hasła");
            }
        }

        private static async Task HandleDeleteChannel(Client client, dynamic data)
        {
            if (!client.IsAuthenticated)
            {
                await SendErrorToClient(client, "Musisz być zalogowany");
                return;
            }

            try
            {
                string channelId = data.ChannelId?.ToString();

                if (!channels.ContainsKey(channelId))
                {
                    await SendErrorToClient(client, "Kanał nie istnieje");
                    return;
                }

                var channel = channels[channelId];

                if (channel.OwnerId != client.Id)
                {
                    await SendErrorToClient(client, "Możesz usunąć tylko swoje kanały");
                    return;
                }

                if (channel.IsGlobal)
                {
                    await SendErrorToClient(client, "Nie można usunąć kanału globalnego");
                    return;
                }

                // Powiadom wszystkich użytkowników na kanale
                var deleteMessage = new ChatMessage
                {
                    Nick = "System",
                    Message = $"Kanał '{channel.Name}' został usunięty przez właściciela",
                    Timestamp = DateTime.Now,
                    MessageType = "channel_deleted",
                    ChannelId = channelId,
                    UserId = "system"
                };

                await BroadcastToChannel(channelId, deleteMessage, null);

                // Usuń kanał z listy użytkowników
                foreach (var c in clients.Where(c => c.JoinedChannels.Contains(channelId)))
                {
                    c.JoinedChannels.Remove(channelId);
                    if (c.CurrentChannelId == channelId)
                    {
                        c.CurrentChannelId = globalChannelId;
                    }
                    await SendChannelListToClient(c);
                }

                // Usuń kanał z własnych kanałów właściciela
                if (users.ContainsKey(client.UserId))
                {
                    users[client.UserId].OwnedChannels.Remove(channelId);
                }

                channels.Remove(channelId);
                SaveChannels();
                SaveUsers();

                await SendSuccessToClient(client, $"Kanał '{channel.Name}' został usunięty");

                Console.WriteLine($"Użytkownik {client.Nick} usunął kanał {channel.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd usuwania kanału: {ex.Message}");
                await SendErrorToClient(client, "Błąd serwera podczas usuwania kanału");
            }
        }

        private static async Task HandleGetOwnedChannels(Client client)
        {
            if (!client.IsAuthenticated)
            {
                await SendErrorToClient(client, "Musisz być zalogowany");
                return;
            }

            try
            {
                var ownedChannels = channels.Values
                    .Where(c => c.OwnerId == client.Id)
                    .Select(c => new ChannelInfo
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Icon = c.Icon,
                        HasPassword = !string.IsNullOrEmpty(c.Password),
                        MemberCount = c.ActiveMembers.Count,
                        IsGlobal = c.IsGlobal,
                        OwnerId = c.OwnerId
                    })
                    .ToList();

                var response = new
                {
                    MessageType = "owned_channels_response",
                    Channels = ownedChannels
                };

                await SendToClient(client, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd pobierania własnych kanałów: {ex.Message}");
                await SendErrorToClient(client, "Błąd serwera podczas pobierania kanałów");
            }
        }


        private static void ShowStartupWarning()
        {
        Console.WriteLine("=====================================");
        Console.WriteLine("⚠️  WAŻNE OSTRZEŻENIE  ⚠️");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("Ta aplikacja zawiera BARDZO DUŻO BŁĘDÓW");
        Console.WriteLine("i prawdopodobnie NIGDY nie otrzyma aktualizacji (jestem zbyt leniwy).");
        Console.WriteLine();
        Console.WriteLine("📅 SERWER PUBLICZNY (02.mikr.us:32237)");
        Console.WriteLine("   PRZESTANIE DZIAŁAĆ 20 WRZEŚNIA 2025");
        Console.WriteLine();
        Console.WriteLine("🔧 Kod źródłowy jest dostępny na GitHub");
        Console.WriteLine("   – możesz stworzyć własną wersję");
        Console.WriteLine();
        Console.WriteLine("❗ Wszystkie wiadomości są wysyłane do serwera w formie tekstowej.");
        Console.WriteLine("   Ten klient NIE jest bezpieczny – każda wiadomość jest zapisywana");
        Console.WriteLine("   na serwerze jako zwykły tekst");
        Console.WriteLine();
        Console.WriteLine("💡 W skrócie: korzystaj tylko z publicznego serwera lub własnego, któremu ufasz.");
        Console.WriteLine();
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.Write("Naciśnij ENTER, aby kontynuować lub Ctrl+C, aby anulować...");
        Console.ReadLine();
        Console.WriteLine();
        }
    }
}