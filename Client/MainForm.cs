using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CzatujiemyClient
{
    public class ChatMessage
    {
        public string Nick { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string MessageType { get; set; }
        public string ChannelId { get; set; }
        public string UserId { get; set; }
    }

    public class ChannelInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool HasPassword { get; set; }
        public int MemberCount { get; set; }
        public bool IsGlobal { get; set; }
        public string OwnerId { get; set; }
    }

    public class ChannelMember
    {
        public string UserId { get; set; }
        public string Nick { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public bool IsMuted { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public partial class MainForm : Form
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected = false;
        private string currentChannelId = "global";
        private List<ChannelInfo> channels = new List<ChannelInfo>();
        private JoinChannelForm activeJoinForm;
        private ChannelSettingsForm activeSettingsForm;
        private AccountSettingsForm activeAccountForm;
        

        public string CurrentNick { get; private set; }
        private string currentPassword;
        private string serverIP;
        private int serverPort;
        private bool isRegister;
        private string currentUserId;
        

        private Timer reconnectTimer;
        private bool isReconnecting = false;
        private string lastNick = "";
        private readonly int maxMessageLength = 500;
        private bool soundEnabled = true;
        private Timer typingTimer;
        private bool isTyping = false;
        private List<string> chatHistory = new List<string>();
        private List<string> messageHistory = new List<string>();
        private int historyIndex = -1;


        private RichTextBox rtbChat;
        private TextBox txtMessage;
        private Button btnConnect;
        private Button btnSend;
        private Label lblStatus;
        private Label lblNick;
        private Label lblMessage;

        // Kontrolki GUI - kanały
        private Panel pnlChannels;
        private ListBox lstChannels;
        private Button btnCreateChannel;
        private Button btnJoinChannel;
        private Button btnChannelSettings;
        private Label lblChannels;
        private Label lblCurrentChannel;
        
        // Nowe kontrolki GUI
        private ListBox lstOnlineUsers;
        private Label lblOnlineUsers;
        private Button btnEmoji;
        private CheckBox chkAutoReconnect;
        private CheckBox chkSoundEnabled;
        private Label lblMessageCounter;
        private Button btnClearChat;
        private Button btnSaveHistory;
        private Label lblTypingStatus;

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
        }

        public MainForm(string nick, string password, string serverIP, int serverPort, bool isRegister) : this()
        {
            this.CurrentNick = nick;
            this.currentPassword = password;
            this.serverIP = serverIP;
            this.serverPort = serverPort;
            this.isRegister = isRegister;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1200, 700);
            this.Text = "Czatujemy 2.0";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 500);
            this.Icon = SystemIcons.Application;
            this.Resize += MainForm_Resize;


            

            lblNick = new Label();
            lblNick.Text = $"Zalogowany jako: {CurrentNick ?? "Użytkownik"}";
            lblNick.Location = new Point(10, 15);
            lblNick.Size = new Size(250, 20);
            lblNick.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblNick.ForeColor = Color.DarkBlue;
            this.Controls.Add(lblNick);


            var btnAccountSettings = new Button();
            btnAccountSettings.Text = "⚙️ Konto";
            btnAccountSettings.Location = new Point(270, 10);
            btnAccountSettings.Size = new Size(95, 35);
            btnAccountSettings.BackColor = Color.FromArgb(52, 73, 94);
            btnAccountSettings.ForeColor = Color.White;
            btnAccountSettings.FlatStyle = FlatStyle.Flat;
            btnAccountSettings.FlatAppearance.BorderSize = 0;
            btnAccountSettings.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnAccountSettings.Click += BtnAccountSettings_Click;
            this.Controls.Add(btnAccountSettings);

            // Przycisk rozłączenia
            btnConnect = new Button();
            btnConnect.Text = "🔌 Rozłącz";
            btnConnect.Location = new Point(375, 10);
            btnConnect.Size = new Size(100, 35);
            btnConnect.BackColor = Color.FromArgb(231, 76, 60);
            btnConnect.ForeColor = Color.White;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.FlatAppearance.BorderSize = 0;
            btnConnect.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(btnConnect);

            // Status połączenia
            lblStatus = new Label();
            lblStatus.Text = "Łączenie...";
            lblStatus.Location = new Point(460, 15);
            lblStatus.Size = new Size(200, 20);
            lblStatus.ForeColor = Color.Orange;
            this.Controls.Add(lblStatus);

            // Auto-reconnect checkbox
            chkAutoReconnect = new CheckBox();
            chkAutoReconnect.Text = "Auto-reconnect";
            chkAutoReconnect.Location = new Point(530, 15);
            chkAutoReconnect.Size = new Size(120, 20);
            chkAutoReconnect.Checked = true;
            this.Controls.Add(chkAutoReconnect);

            // Dźwięki checkbox
            chkSoundEnabled = new CheckBox();
            chkSoundEnabled.Text = "Dźwięki";
            chkSoundEnabled.Location = new Point(670, 15);
            chkSoundEnabled.Size = new Size(80, 20);
            chkSoundEnabled.Checked = true;
            chkSoundEnabled.CheckedChanged += (s, e) => soundEnabled = chkSoundEnabled.Checked;
            this.Controls.Add(chkSoundEnabled);

            // === PANEL KANAŁÓW (LEWA STRONA) ===
            
            pnlChannels = new Panel();
            pnlChannels.Location = new Point(10, 50);
            pnlChannels.Size = new Size(200, 500);
            pnlChannels.BorderStyle = BorderStyle.FixedSingle;
            pnlChannels.BackColor = Color.FromArgb(236, 240, 241);
            this.Controls.Add(pnlChannels);

            // Label kanałów
            lblChannels = new Label();
            lblChannels.Text = "Kanały:";
            lblChannels.Location = new Point(5, 5);
            lblChannels.Size = new Size(60, 20);
            lblChannels.Font = new Font("Arial", 9, FontStyle.Bold);
            pnlChannels.Controls.Add(lblChannels);

            // Lista kanałów
            lstChannels = new ListBox();
            lstChannels.Location = new Point(5, 25);
            lstChannels.Size = new Size(188, 350);
            lstChannels.DrawMode = DrawMode.OwnerDrawFixed;
            lstChannels.ItemHeight = 25;
            lstChannels.DrawItem += LstChannels_DrawItem;
            lstChannels.SelectedIndexChanged += LstChannels_SelectedIndexChanged;
            pnlChannels.Controls.Add(lstChannels);

            // Przycisk tworzenia kanału
            btnCreateChannel = new Button();
            btnCreateChannel.Text = "➕ Utwórz";
            btnCreateChannel.Location = new Point(5, 385);
            btnCreateChannel.Size = new Size(90, 35);
            btnCreateChannel.BackColor = Color.FromArgb(46, 204, 113);
            btnCreateChannel.ForeColor = Color.White;
            btnCreateChannel.FlatStyle = FlatStyle.Flat;
            btnCreateChannel.FlatAppearance.BorderSize = 0;
            btnCreateChannel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCreateChannel.Click += BtnCreateChannel_Click;
            btnCreateChannel.Enabled = false;
            pnlChannels.Controls.Add(btnCreateChannel);

            // Przycisk dołączania do kanału
            btnJoinChannel = new Button();
            btnJoinChannel.Text = "⚡ Dołącz";
            btnJoinChannel.Location = new Point(100, 385);
            btnJoinChannel.Size = new Size(90, 35);
            btnJoinChannel.BackColor = Color.FromArgb(52, 152, 219);
            btnJoinChannel.ForeColor = Color.White;
            btnJoinChannel.FlatStyle = FlatStyle.Flat;
            btnJoinChannel.FlatAppearance.BorderSize = 0;
            btnJoinChannel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnJoinChannel.Click += BtnJoinChannel_Click;
            btnJoinChannel.Enabled = false;
            pnlChannels.Controls.Add(btnJoinChannel);

            // Przycisk ustawień kanału
            btnChannelSettings = new Button();
            btnChannelSettings.Text = "⚙️ Ustawienia kanału";
            btnChannelSettings.Location = new Point(5, 425);
            btnChannelSettings.Size = new Size(185, 35);
            btnChannelSettings.BackColor = Color.FromArgb(155, 89, 182);
            btnChannelSettings.ForeColor = Color.White;
            btnChannelSettings.FlatStyle = FlatStyle.Flat;
            btnChannelSettings.FlatAppearance.BorderSize = 0;
            btnChannelSettings.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnChannelSettings.Click += BtnChannelSettings_Click;
            btnChannelSettings.Enabled = true; // ZAWSZE aktywny
            pnlChannels.Controls.Add(btnChannelSettings);

            // === PANEL UŻYTKOWNIKÓW ONLINE (PRAWA STRONA) ===
            
            // Label użytkownicy online
            lblOnlineUsers = new Label();
            lblOnlineUsers.Text = "Online (0):";
            lblOnlineUsers.Location = new Point(1000, 55);
            lblOnlineUsers.Size = new Size(100, 20);
            lblOnlineUsers.Font = new Font("Arial", 9, FontStyle.Bold);
            this.Controls.Add(lblOnlineUsers);

            // Lista użytkowników online
            lstOnlineUsers = new ListBox();
            lstOnlineUsers.Location = new Point(1000, 95);
            lstOnlineUsers.Size = new Size(180, 200);
            lstOnlineUsers.Font = new Font("Segoe UI", 9);
            lstOnlineUsers.BackColor = Color.FromArgb(250, 250, 250);
            lstOnlineUsers.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(lstOnlineUsers);

            // === GŁÓWNY PANEL CZATU (ŚRODEK) ===
            
            // Label aktualnego kanału
            lblCurrentChannel = new Label();
            lblCurrentChannel.Text = "🌍 Globalny";
            lblCurrentChannel.Location = new Point(220, 55);
            lblCurrentChannel.Size = new Size(400, 25);
            lblCurrentChannel.Font = new Font("Arial", 10, FontStyle.Bold);
            lblCurrentChannel.ForeColor = Color.DarkBlue;
            this.Controls.Add(lblCurrentChannel);

            // Obszar wyświetlania czatu
            rtbChat = new RichTextBox();
            rtbChat.Location = new Point(220, 80);
            rtbChat.Size = new Size(760, 500);
            rtbChat.ReadOnly = true;
            rtbChat.BackColor = Color.FromArgb(253, 254, 255);
            rtbChat.BorderStyle = BorderStyle.FixedSingle;
            rtbChat.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbChat.Font = new Font("Segoe UI", 9);
            rtbChat.DetectUrls = true;
            rtbChat.LinkClicked += RtbChat_LinkClicked;
            this.Controls.Add(rtbChat);

            // Przycisk czyszczenia czatu
            btnClearChat = new Button();
            btnClearChat.Text = "🗑️ Wyczyść";
            btnClearChat.Location = new Point(1000, 305);
            btnClearChat.Size = new Size(85, 30);
            btnClearChat.BackColor = Color.IndianRed;
            btnClearChat.ForeColor = Color.White;
            btnClearChat.FlatStyle = FlatStyle.Flat;
            btnClearChat.Click += BtnClearChat_Click;
            this.Controls.Add(btnClearChat);

            // Przycisk zapisywania historii
            btnSaveHistory = new Button();
            btnSaveHistory.Text = "💾 Zapisz";
            btnSaveHistory.Location = new Point(1095, 305);
            btnSaveHistory.Size = new Size(85, 30);
            btnSaveHistory.BackColor = Color.ForestGreen;
            btnSaveHistory.ForeColor = Color.White;
            btnSaveHistory.FlatStyle = FlatStyle.Flat;
            btnSaveHistory.Click += BtnSaveHistory_Click;
            this.Controls.Add(btnSaveHistory);

            // Status "pisze..."
            lblTypingStatus = new Label();
            lblTypingStatus.Text = "";
            lblTypingStatus.Location = new Point(220, 585);
            lblTypingStatus.Size = new Size(300, 15);
            lblTypingStatus.ForeColor = Color.Gray;
            lblTypingStatus.Font = new Font("Arial", 8, FontStyle.Italic);
            this.Controls.Add(lblTypingStatus);

            // Licznik znaków
            lblMessageCounter = new Label();
            lblMessageCounter.Text = "0/500";
            lblMessageCounter.Location = new Point(220, 590);
            lblMessageCounter.Size = new Size(60, 20);
            lblMessageCounter.ForeColor = Color.Gray;
            this.Controls.Add(lblMessageCounter);

            // Label dla wiadomości
            lblMessage = new Label();
            lblMessage.Text = "Wiadomość:";
            lblMessage.Location = new Point(220, 615);
            lblMessage.Size = new Size(70, 20);
            this.Controls.Add(lblMessage);

            // Pole tekstowe dla wiadomości
            txtMessage = new TextBox();
            txtMessage.Location = new Point(295, 612);
            txtMessage.Size = new Size(550, 30);
            txtMessage.Font = new Font("Segoe UI", 10);
            txtMessage.BackColor = Color.FromArgb(248, 249, 250);
            txtMessage.BorderStyle = BorderStyle.FixedSingle;
            txtMessage.KeyDown += TxtMessage_KeyDown;
            txtMessage.TextChanged += TxtMessage_TextChanged;
            txtMessage.Enabled = false;
            txtMessage.MaxLength = maxMessageLength;
            txtMessage.PlaceholderText = "Napisz wiadomość...";
            this.Controls.Add(txtMessage);

            // Przycisk emoji
            btnEmoji = new Button();
            btnEmoji.Text = "😀";
            btnEmoji.Location = new Point(850, 610);
            btnEmoji.Size = new Size(40, 35);
            btnEmoji.BackColor = Color.FromArgb(241, 196, 15);
            btnEmoji.ForeColor = Color.White;
            btnEmoji.FlatStyle = FlatStyle.Flat;
            btnEmoji.FlatAppearance.BorderSize = 0;
            btnEmoji.Font = new Font("Segoe UI Emoji", 12);
            btnEmoji.Click += BtnEmoji_Click;
            btnEmoji.Enabled = false;
            this.Controls.Add(btnEmoji);

            // Przycisk wysyłania
            btnSend = new Button();
            btnSend.Text = "📤 Wyślij";
            btnSend.Location = new Point(895, 610);
            btnSend.Size = new Size(85, 35);
            btnSend.BackColor = Color.FromArgb(231, 76, 60);
            btnSend.ForeColor = Color.White;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSend.Click += BtnSend_Click;
            btnSend.Enabled = false;
            this.Controls.Add(btnSend);

            // Inicjalizacja timerów
            reconnectTimer = new Timer();
            reconnectTimer.Interval = 5000; // 5 sekund
            reconnectTimer.Tick += ReconnectTimer_Tick;

            typingTimer = new Timer();
            typingTimer.Interval = 2000; // 2 sekundy
            typingTimer.Tick += TypingTimer_Tick;
            
            // Automatyczne łączenie po załadowaniu formy
            this.Load += MainForm_Load;
        }

        private void TxtMessage_TextChanged(object sender, EventArgs e)
        {
            int currentLength = txtMessage.Text.Length;
            lblMessageCounter.Text = $"{currentLength}/{maxMessageLength}";
            
            if (currentLength > maxMessageLength * 0.9)
                lblMessageCounter.ForeColor = Color.Red;
            else if (currentLength > maxMessageLength * 0.7)
                lblMessageCounter.ForeColor = Color.Orange;
            else
                lblMessageCounter.ForeColor = Color.Gray;

            // Obsługa statusu "pisze..."
            if (isConnected && currentLength > 0 && !isTyping)
            {
                isTyping = true;
                SendTypingStatus(true);
            }
            
            if (currentLength > 0)
            {
                typingTimer.Stop();
                typingTimer.Start();
            }
        }

        private void BtnEmoji_Click(object sender, EventArgs e)
        {
            var emojiForm = new EmojiPicker();
            if (emojiForm.ShowDialog() == DialogResult.OK)
            {
                txtMessage.Text += emojiForm.SelectedEmoji;
                txtMessage.Focus();
                txtMessage.SelectionStart = txtMessage.Text.Length;
            }
        }

        private void RtbChat_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.LinkText,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddMessageToChat("System", $"Nie można otworzyć linku: {ex.Message}", DateTime.Now, Color.Red);
            }
        }

        private void BtnClearChat_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Czy na pewno chcesz wyczyścić okno czatu?", "Potwierdzenie", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                rtbChat.Clear();
                AddMessageToChat("System", "Okno czatu zostało wyczyszczone", DateTime.Now, Color.Blue);
            }
        }

        private void BtnSaveHistory_Click(object sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Pliki tekstowe|*.txt|Wszystkie pliki|*.*";
                saveDialog.FileName = $"czat_{currentChannelId}_{DateTime.Now:yyyy-MM-dd_HH-mm}.txt";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, rtbChat.Text);
                    AddMessageToChat("System", $"Historia czatu zapisana do: {saveDialog.FileName}", DateTime.Now, Color.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisywania historii: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if (isTyping)
            {
                isTyping = false;
                SendTypingStatus(false);
            }
            typingTimer.Stop();
        }

        private async void SendTypingStatus(bool typing)
        {
            try
            {
                var typingData = new
                {
                    MessageType = "typing",
                    IsTyping = typing,
                    ChannelId = currentChannelId
                };
                await SendToServer(typingData);
            }
            catch
            {
                // Ignoruj błędy typing status
            }
        }

        private async void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!isConnected && chkAutoReconnect.Checked && !isReconnecting && !string.IsNullOrEmpty(CurrentNick))
            {
                isReconnecting = true;
                lblStatus.Text = "Łączenie...";
                lblStatus.ForeColor = Color.Orange;
                
                try
                {
                    await AttemptReconnect();
                }
                catch
                {
                    // Spróbuj ponownie za 5 sekund
                }
                finally
                {
                    isReconnecting = false;
                }
            }
        }

        private async Task AttemptReconnect()
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIP, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;

                // Wyślij żądanie logowania
                var loginData = new
                {
                    MessageType = "login",
                    Nick = CurrentNick,
                    Password = currentPassword
                };
                await SendToServer(loginData);

                // Zaktualizuj GUI
                lblStatus.Text = "Połączony (auto)";
                lblStatus.ForeColor = Color.Green;
                txtMessage.Enabled = true;
                btnSend.Enabled = true;
                btnEmoji.Enabled = true;
                btnCreateChannel.Enabled = true;
                btnJoinChannel.Enabled = true;

                AddMessageToChat("System", "Automatycznie połączono ponownie", DateTime.Now, Color.Green);

                // Rozpocznij odbieranie wiadomości
                _ = Task.Run(() => ReceiveMessages());
                
                // Poproś o listę użytkowników online
                await RequestOnlineUsers();
                
                reconnectTimer.Stop();
            }
            catch
            {
                // Spróbuj ponownie później
                throw;
            }
        }

        private void LstChannels_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= channels.Count) return;

            var channel = channels[e.Index];
            e.DrawBackground();

            // Kolor tła dla aktualnego kanału
            if (channel.Id == currentChannelId)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.LightBlue), e.Bounds);
            }

            // Rysowanie ikony i nazwy
            string displayText = $"{channel.Icon} {channel.Name}";
            if (channel.HasPassword) displayText += " 🔒";
            displayText += $" ({channel.MemberCount})";

            var textBrush = new SolidBrush(e.ForeColor);
            e.Graphics.DrawString(displayText, e.Font, textBrush, e.Bounds.Location);
            
            e.DrawFocusRectangle();
        }

        private async void LstChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstChannels.SelectedIndex >= 0 && lstChannels.SelectedIndex < channels.Count)
            {
                var selectedChannel = channels[lstChannels.SelectedIndex];
                if (selectedChannel.Id != currentChannelId)
                {
                    await SwitchChannel(selectedChannel.Id);
                }
            }
        }

        private async void BtnCreateChannel_Click(object sender, EventArgs e)
        {
            var createForm = new CreateChannelForm();
            if (createForm.ShowDialog() == DialogResult.OK)
            {
                await CreateChannel(createForm.ChannelName, createForm.ChannelIcon, createForm.ChannelPassword);
            }
        }

        private async void BtnJoinChannel_Click(object sender, EventArgs e)
        {
            activeJoinForm = new JoinChannelForm(this);
            if (activeJoinForm.ShowDialog() == DialogResult.OK)
            {
                await JoinChannel(activeJoinForm.SelectedChannelId, activeJoinForm.Password);
            }
            activeJoinForm = null;
        }

        private void BtnChannelSettings_Click(object sender, EventArgs e)
        {
            var currentChannel = channels.FirstOrDefault(c => c.Id == currentChannelId);
            if (currentChannel != null)
            {
                try
                {
                    activeSettingsForm = new ChannelSettingsForm(currentChannel, this);
                    activeSettingsForm.ShowDialog();
                    activeSettingsForm = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd otwierania ustawień kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie można otworzyć ustawień - brak aktualnego kanału", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                // Rozłącz się
                Disconnect();
            }
            else
            {
                // Próbuj połączyć się ponownie
                await ConnectToServer();
            }
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            await SendMessageFromInput();
        }

        private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                await SendMessageFromInput();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && e.Control)
            {
                // Ctrl+Up - poprzednia wiadomość z historii
                if (messageHistory.Count > 0 && historyIndex < messageHistory.Count - 1)
                {
                    historyIndex++;
                    txtMessage.Text = messageHistory[messageHistory.Count - 1 - historyIndex];
                    txtMessage.SelectionStart = txtMessage.Text.Length;
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down && e.Control)
            {
                // Ctrl+Down - następna wiadomość z historii
                if (historyIndex > 0)
                {
                    historyIndex--;
                    txtMessage.Text = messageHistory[messageHistory.Count - 1 - historyIndex];
                    txtMessage.SelectionStart = txtMessage.Text.Length;
                }
                else if (historyIndex == 0)
                {
                    historyIndex = -1;
                    txtMessage.Text = "";
                }
                e.Handled = true;
            }
        }

        private async Task SendMessageFromInput()
        {
            if (!isConnected || string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            try
            {
                // Dodaj do historii wiadomości
                if (!string.IsNullOrWhiteSpace(txtMessage.Text))
                {
                    messageHistory.Add(txtMessage.Text);
                    if (messageHistory.Count > 50) // Limit historii
                    {
                        messageHistory.RemoveAt(0);
                    }
                    historyIndex = -1;
                }

                var messageData = new
                {
                    MessageType = "message",
                    Message = txtMessage.Text
                };

                await SendToServer(messageData);
                txtMessage.Clear();
                
                // Dźwięk wysyłania wiadomości
                PlayNotificationSound("send");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wysyłania wiadomości: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SwitchChannel(string channelId)
        {
            Console.WriteLine($"[DEBUG] SwitchChannel wywołane: channelId={channelId}, currentChannelId={currentChannelId}");
            
            if (currentChannelId == channelId) 
            {
                Console.WriteLine($"[DEBUG] Przełączenie anulowane - już na tym kanale");
                return; // Unikaj niepotrzebnych przełączeń
            }
            
            // Wyczyść czat przed przełączeniem
            rtbChat.Clear();
            
            // Aktualizuj currentChannelId natychmiast, żeby uniknąć problemów z synchronizacją
            string oldChannelId = currentChannelId;
            currentChannelId = channelId;
            Console.WriteLine($"[DEBUG] currentChannelId zmienione z {oldChannelId} na {currentChannelId}");
            
            // Aktualizuj etykietę aktualnego kanału
            var activeChannel = channels.FirstOrDefault(c => c.Id == currentChannelId);
            if (activeChannel != null)
            {
                lblCurrentChannel.Text = $"{activeChannel.Icon} {activeChannel.Name}";
                Console.WriteLine($"[DEBUG] Etykieta kanału zaktualizowana: {activeChannel.Icon} {activeChannel.Name}");
                
                // Przycisk ustawień jest zawsze aktywny
            }
            else
            {
                lblCurrentChannel.Text = "🌍 Globalny";
                Console.WriteLine($"[DEBUG] Ustawiono etykietę na Globalny");
                
                // Przycisk ustawień jest zawsze aktywny
            }
            
            // Poproś serwer o przełączenie kanału
            var requestData = new
            {
                MessageType = "switch_channel",
                ChannelId = channelId
            };
            Console.WriteLine($"[DEBUG] Wysyłanie żądania switch_channel do serwera: {channelId}");
            await SendToServer(requestData);
        }

        private async Task CreateChannel(string name, string icon, string password)
        {
            try
            {
                var createData = new
                {
                    MessageType = "create_channel",
                    Name = name,
                    Icon = icon,
                    Password = password
                };
                
                await SendToServer(createData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd tworzenia kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task JoinChannel(string channelId, string password)
        {
            try
            {
                var joinData = new
                {
                    MessageType = "join_channel",
                    ChannelId = channelId,
                    Password = password
                };
                
                await SendToServer(joinData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd dołączania do kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateChannelList(List<ChannelInfo> newChannels, string currentChannel)
        {
            Console.WriteLine($"[DEBUG] UpdateChannelList wywołane: newChannels.Count={newChannels?.Count}, currentChannel='{currentChannel}'");
            
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateChannelList(newChannels, currentChannel)));
                return;
            }

            var previousChannelId = currentChannelId;
            channels = newChannels;
            
            // Ustaw currentChannelId lub zachowaj istniejący
            if (!string.IsNullOrEmpty(currentChannel))
            {
                // Aktualizuj tylko jeśli serwer wysyła inny kanał niż aktualny
                // lub jeśli nie mamy jeszcze ustawionego kanału
                if (string.IsNullOrEmpty(currentChannelId) || currentChannelId != currentChannel)
                {
                    currentChannelId = currentChannel;
                }
            }
            else if (string.IsNullOrEmpty(currentChannelId) || !channels.Any(c => c.Id == currentChannelId))
            {
                currentChannelId = "global";  // Domyślnie global
            }
            
            lstChannels.Items.Clear();
            foreach (var channel in channels)
            {
                lstChannels.Items.Add(channel);
            }
            
            // Aktualizuj informacje o aktualnym kanale
            var activeChannel = channels.FirstOrDefault(c => c.Id == currentChannelId);
            if (activeChannel != null)
            {
                lblCurrentChannel.Text = $"{activeChannel.Icon} {activeChannel.Name}";
                
                // Przycisk ustawień jest zawsze aktywny
                
                // Wybierz aktualny kanał na liście
                for (int i = 0; i < channels.Count; i++)
                {
                    if (channels[i].Id == currentChannelId)
                    {
                        lstChannels.SelectedIndex = i;
                        break;
                    }
                }
                
                // Przeładuj historię jeśli kanał się zmienił
                if (previousChannelId != currentChannelId)
                {
                    _ = Task.Run(async () => 
                    {
                        await Task.Delay(300); // Opóźnienie dla stabilności
                        // Wyczyść czat i poproś o historię
                        this.Invoke(new Action(() => rtbChat.Clear()));
                        
                        var requestData = new
                        {
                            MessageType = "get_channel_history",
                            ChannelId = currentChannelId
                        };
                        await SendToServer(requestData);
                    });
                }
            }
            else
            {
                lblCurrentChannel.Text = "🌍 Globalny";
                currentChannelId = "global";
                // Przycisk ustawień jest zawsze aktywny
            }
            
            lstChannels.Invalidate();
        }

        private void UpdateOnlineUsersList(List<string> onlineUsers)
        {
            lstOnlineUsers.Items.Clear();
            foreach (var user in onlineUsers)
            {
                lstOnlineUsers.Items.Add($"🟢 {user}");
            }
            lblOnlineUsers.Text = $"Online ({onlineUsers.Count}):";
        }

        private readonly List<string> typingUsers = new List<string>();
        
        private void UpdateTypingStatus(string user, bool isTyping)
        {
            if (isTyping && !typingUsers.Contains(user))
            {
                typingUsers.Add(user);
            }
            else if (!isTyping && typingUsers.Contains(user))
            {
                typingUsers.Remove(user);
            }

            // Aktualizuj label
            if (typingUsers.Count == 0)
            {
                lblTypingStatus.Text = "";
            }
            else if (typingUsers.Count == 1)
            {
                lblTypingStatus.Text = $"{typingUsers[0]} pisze...";
            }
            else if (typingUsers.Count <= 3)
            {
                lblTypingStatus.Text = $"{string.Join(", ", typingUsers)} piszą...";
            }
            else
            {
                lblTypingStatus.Text = $"{typingUsers.Count} użytkowników pisze...";
            }
        }

        private void PlayNotificationSound(string soundType = "message")
        {
            if (soundEnabled)
            {
                try
                {
                    // Najpierw spróbuj w katalogu wykonywalnym
                    string soundPath = Path.Combine(Application.StartupPath, "sounds", $"{soundType}.wav");
                    Console.WriteLine($"[DEBUG] Szukam dźwięku w: {soundPath}");
                    
                    // Jeśli nie ma w katalogu wykonywalnym, spróbuj w katalogu głównym projektu
                    if (!File.Exists(soundPath))
                    {
                        // Idź 4 poziomy w górę z bin\Debug\net6.0-windows do katalogu głównego
                        string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..", ".."));
                        soundPath = Path.Combine(projectRoot, "sounds", $"{soundType}.wav");
                        Console.WriteLine($"[DEBUG] Nie znaleziono, próbuję w: {soundPath}");
                    }
                    
                    if (File.Exists(soundPath))
                    {
                        Console.WriteLine($"[DEBUG] Odtwarzam dźwięk: {soundPath}");
                        // Użyj SoundPlayer do odtworzenia pliku WAV
                        var player = new System.Media.SoundPlayer(soundPath);
                        player.Play();
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Plik dźwiękowy nie istnieje, używam systemowego dźwięku");
                        // Fallback na systemowe dźwięki
                        switch (soundType)
                        {
                            case "join":
                                System.Media.SystemSounds.Exclamation.Play();
                                break;
                            case "leave":
                                System.Media.SystemSounds.Hand.Play();
                                break;
                            case "send":
                                System.Media.SystemSounds.Question.Play();
                                break;
                            default:
                                System.Media.SystemSounds.Asterisk.Play();
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Błąd odtwarzania dźwięku: {ex.Message}");
                    // Ignoruj błędy dźwięku - fallback na prosty systemowy dźwięk
                    System.Media.SystemSounds.Beep.Play();
                }
            }
        }

        public async Task RequestAvailableChannels()
        {
            try
            {
                var requestData = new
                {
                    MessageType = "get_channels"
                };
                await SendToServer(requestData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania listy kanałów: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task RequestOnlineUsers()
        {
            try
            {
                var requestData = new
                {
                    MessageType = "get_online_users"
                };
                await SendToServer(requestData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd pobierania listy użytkowników online: {ex.Message}");
            }
        }

        public async Task RequestChannelMembers(string channelId)
        {
            try
            {
                var requestData = new
                {
                    MessageType = "get_channel_members",
                    ChannelId = channelId
                };
                await SendToServer(requestData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania członków kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task UpdateChannel(string channelId, string name, string icon, string password)
        {
            try
            {
                var updateData = new
                {
                    MessageType = "update_channel",
                    ChannelId = channelId,
                    Name = name,
                    Icon = icon,
                    Password = password
                };
                await SendToServer(updateData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task KickUser(string channelId, string userId)
        {
            try
            {
                var kickData = new
                {
                    MessageType = "kick_user",
                    ChannelId = channelId,
                    UserId = userId
                };
                await SendToServer(kickData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wykopywania użytkownika: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task BanUser(string channelId, string userId)
        {
            try
            {
                var banData = new
                {
                    MessageType = "ban_user",
                    ChannelId = channelId,
                    UserId = userId
                };
                await SendToServer(banData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd banowania użytkownika: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task MuteUser(string channelId, string userId)
        {
            try
            {
                var muteData = new
                {
                    MessageType = "mute_user",
                    ChannelId = channelId,
                    UserId = userId
                };
                await SendToServer(muteData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wyciszania użytkownika: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task UnmuteUser(string channelId, string userId)
        {
            try
            {
                var unmuteData = new
                {
                    MessageType = "unmute_user",
                    ChannelId = channelId,
                    UserId = userId
                };
                await SendToServer(unmuteData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odciszania użytkownika: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task UnbanUser(string channelId, string userId)
        {
            try
            {
                var unbanData = new
                {
                    MessageType = "unban_user",
                    ChannelId = channelId,
                    UserId = userId
                };
                await SendToServer(unbanData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odbanowywania użytkownika: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendToServer(object data)
        {
            if (!isConnected || stream == null) return;

            try
            {
                string json = JsonConvert.SerializeObject(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                AddMessageToChat("System", $"Błąd wysyłania: {ex.Message}", DateTime.Now, Color.Red);
            }
        }

        private async Task ReceiveMessages()
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (isConnected && tcpClient?.Connected == true && stream != null)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string jsonData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    try
                    {
                        var data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                        string messageType = data.MessageType?.ToString();

                        ProcessServerMessage(messageType, data);
                    }
                    catch (JsonException)
                    {
                        // Ignoruj nieprawidłowe wiadomości JSON
                    }
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                {
                    this.Invoke(new Action(() =>
                    {
                        AddMessageToChat("System", $"Utracono połączenie: {ex.Message}", DateTime.Now, Color.Red);
                        Disconnect();
                    }));
                }
            }
        }

        private void ProcessServerMessage(string messageType, dynamic data)
        {
            switch (messageType)
            {
                case "message":
                case "join":
                case "leave":
                case "kick":
                case "ban":
                case "mute":
                case "unmute":
                    var message = JsonConvert.DeserializeObject<ChatMessage>(data.ToString());
                    
                    // Debug: loguj informacje o wiadomości
                    Console.WriteLine($"[DEBUG] Otrzymano wiadomość: ChannelId={message.ChannelId}, CurrentChannelId={currentChannelId}, Nick={message.Nick}, Message={message.Message}");
                    
                    if (message.ChannelId == currentChannelId)
                    {
                        Color color = GetMessageColor(message.MessageType);
                        this.Invoke(new Action(() =>
                        {
                            AddMessageToChat(message.Nick, message.Message, message.Timestamp, color);
                            
                            // Dźwięk powiadomienia (tylko dla wiadomości innych użytkowników)
                            if (message.MessageType == "message" && message.Nick != CurrentNick)
                            {
                                PlayNotificationSound("message");
                            }
                            else if (message.MessageType == "join")
                            {
                                PlayNotificationSound("join");
                            }
                            else if (message.MessageType == "leave")
                            {
                                PlayNotificationSound("leave");
                            }
                        }));
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Wiadomość odrzucona - różne kanały: message.ChannelId={message.ChannelId}, currentChannelId={currentChannelId}");
                    }
                    break;

                case "history":
                    if (data?.Messages != null)
                    {
                        var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(data.Messages.ToString());
                        this.Invoke(new Action(() =>
                        {
                            rtbChat.Clear();
                            AddMessageToChat("System", "=== Historia kanału ===", DateTime.Now, Color.Purple);
                            foreach (var msg in messages)
                            {
                                Color color = GetMessageColor(msg.MessageType);
                                AddMessageToChat(msg.Nick, msg.Message, msg.Timestamp, color);
                            }
                            if (messages.Count == 0)
                            {
                                AddMessageToChat("System", "Brak wiadomości w tym kanale", DateTime.Now, Color.Gray);
                            }
                        }));
                    }
                    break;

                case "channel_list":
                    if (data?.Channels != null)
                    {
                        var channelList = JsonConvert.DeserializeObject<List<ChannelInfo>>(data.Channels.ToString());
                        string currentChannel = data.CurrentChannelId?.ToString();
                        this.Invoke(new Action(() =>
                        {
                            UpdateChannelList(channelList, currentChannel);
                        }));
                    }
                    break;

                case "available_channels":
                    if (data?.Channels != null)
                    {
                        var availableChannels = JsonConvert.DeserializeObject<List<ChannelInfo>>(data.Channels.ToString());
                        this.Invoke(new Action(() =>
                        {
                            if (activeJoinForm != null)
                            {
                                activeJoinForm.UpdateAvailableChannels(availableChannels);
                            }
                        }));
                    }
                    break;

                case "online_users":
                    if (data?.Users != null)
                    {
                        var onlineUsers = JsonConvert.DeserializeObject<List<string>>(data.Users.ToString());
                        this.Invoke(new Action(() =>
                        {
                            UpdateOnlineUsersList(onlineUsers);
                        }));
                    }
                    break;

                case "typing":
                    string typingUser = data.User?.ToString();
                    bool isUserTyping = data.IsTyping == true;
                    string channelId = data.ChannelId?.ToString();
                    
                    this.Invoke(new Action(() =>
                    {
                        if (channelId == currentChannelId)
                        {
                            UpdateTypingStatus(typingUser, isUserTyping);
                        }
                    }));
                    break;

                case "channel_members":
                    if (data?.Members != null)
                    {
                        var members = JsonConvert.DeserializeObject<List<ChannelMember>>(data.Members.ToString());
                        this.Invoke(new Action(() =>
                        {
                            if (activeSettingsForm != null)
                            {
                                activeSettingsForm.UpdateChannelMembers(members);
                            }
                        }));
                    }
                    break;

                case "login_response":
                    HandleLoginResponse(data?.Response);
                    break;
                
                case "register_response":
                    HandleRegisterResponse(data?.Response);
                    break;
                
                case "owned_channels_response":
                    HandleOwnedChannelsResponse(data?.Channels);
                    break;

                case "channel_switched":
                    string switchedChannelId = data?.ChannelId?.ToString();
                    string channelName = data?.ChannelName?.ToString();
                    string channelIcon = data?.ChannelIcon?.ToString();
                    this.Invoke(new Action(() =>
                    {
                        // Upewnij się, że currentChannelId jest zsynchronizowane z serwerem
                        if (!string.IsNullOrEmpty(switchedChannelId))
                        {
                            currentChannelId = switchedChannelId;
                            lblCurrentChannel.Text = $"{channelIcon} {channelName}";
                            
                            // Zaktualizuj zaznaczenie na liście kanałów
                            for (int i = 0; i < channels.Count; i++)
                            {
                                if (channels[i].Id == currentChannelId)
                                {
                                    lstChannels.SelectedIndex = i;
                                    
                                    // Przycisk ustawień jest zawsze aktywny
                                    break;
                                }
                            }
                        }
                        
                        AddMessageToChat("System", $"Przełączono na kanał: {channelIcon} {channelName}", DateTime.Now, Color.Green);
                    }));
                    break;

                case "error":
                    string error = data.Message?.ToString();
                    this.Invoke(new Action(() =>
                    {
                        AddMessageToChat("System", $"Błąd: {error}", DateTime.Now, Color.Red);
                    }));
                    break;

                case "success":
                    string success = data.Message?.ToString();
                    this.Invoke(new Action(() =>
                    {
                        AddMessageToChat("System", success, DateTime.Now, Color.Green);
                    }));
                    break;

                default:
                    // Ignoruj nieznane typy wiadomości
                    break;
            }
        }

        private Color GetMessageColor(string messageType)
        {
            return messageType switch
            {
                "join" => Color.Green,
                "leave" => Color.Orange,
                "message" => Color.Black,
                _ => Color.Gray
            };
        }

        private void AddMessageToChat(string nick, string message, DateTime timestamp, Color color)
        {
            if (rtbChat.InvokeRequired)
            {
                rtbChat.Invoke(new Action(() => AddMessageToChat(nick, message, timestamp, color)));
                return;
            }

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionLength = 0;
            rtbChat.SelectionColor = Color.Gray;
            rtbChat.AppendText($"[{timestamp:HH:mm:ss}] ");
            
            // Formatowanie specjalne dla różnych typów użytkowników
            if (nick.Contains("👑")) // Owner
            {
                rtbChat.SelectionColor = Color.Purple;
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            }
            else if (nick.Contains("⭐")) // Admin  
            {
                rtbChat.SelectionColor = Color.DarkBlue;
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            }
            else if (nick.Contains("🛡️")) // Moderator
            {
                rtbChat.SelectionColor = Color.DarkGreen;
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Italic);
            }
            else if (nick == "System")
            {
                rtbChat.SelectionColor = Color.Red;
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            }
            else
            {
                rtbChat.SelectionColor = Color.Blue;
                rtbChat.SelectionFont = rtbChat.Font;
            }
            
            rtbChat.AppendText($"{nick}: ");
            
            rtbChat.SelectionColor = color;
            rtbChat.SelectionFont = rtbChat.Font;
            rtbChat.AppendText($"{message}\n");
            
            rtbChat.SelectionColor = rtbChat.ForeColor;
            rtbChat.ScrollToCaret();
            
            // Powiadomienie w pasku tytułu
            if (nick != "System" && nick != lastNick && !this.ContainsFocus)
            {
                FlashWindow();
            }
            
            // Zapisz do historii lokalnej
            chatHistory.Add($"[{timestamp:yyyy-MM-dd HH:mm:ss}] {nick}: {message}");
            if (chatHistory.Count > 1000) // Limit historii
            {
                chatHistory.RemoveAt(0);
            }
        }

        // Win32 API dla migania okna
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private void FlashWindow()
        {
            try
            {
                FlashWindow(this.Handle, true);
                this.Text = "💬 Czatujemy 2.0 - Nowa wiadomość!";
                
                // Przywróć tytuł po 3 sekundach
                var titleTimer = new Timer();
                titleTimer.Interval = 3000;
                titleTimer.Tick += (s, e) =>
                {
                    this.Text = "Czatujemy 2.0 - Klient z kanałami";
                    titleTimer.Stop();
                    titleTimer.Dispose();
                };
                titleTimer.Start();
            }
            catch
            {
                // Ignoruj błędy Win32 API
            }
        }

        private void Disconnect()
        {
            isConnected = false;

            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas rozłączania: {ex.Message}");
            }

            // Zaktualizuj GUI
            btnConnect.Text = "Połącz ponownie";
            lblStatus.Text = "Rozłączony";
            lblStatus.ForeColor = Color.Red;
            txtMessage.Enabled = false;
            btnSend.Enabled = false;
            btnEmoji.Enabled = false;
            btnCreateChannel.Enabled = false;
            btnJoinChannel.Enabled = false;
            // btnChannelSettings pozostaje zawsze aktywny

            // Wyczyść listę kanałów i użytkowników
            channels.Clear();
            lstChannels.Items.Clear();
            lstOnlineUsers.Items.Clear();
            lblOnlineUsers.Text = "Online (0):";
            currentChannelId = "global";
            lblCurrentChannel.Text = "🌍 Globalny";

            AddMessageToChat("System", "Rozłączono z serwerem", DateTime.Now, Color.Red);
            
            // Uruchom auto-reconnect jeśli włączony
            if (chkAutoReconnect.Checked && !string.IsNullOrEmpty(CurrentNick))
            {
                reconnectTimer.Start();
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Automatyczne łączenie po załadowaniu formy
            if (!string.IsNullOrEmpty(CurrentNick) && !string.IsNullOrEmpty(currentPassword))
            {
                await ConnectToServer();
            }
        }

        private async Task ConnectToServer()
        {
            try
            {
                lblStatus.Text = "Łączenie...";
                lblStatus.ForeColor = Color.Orange;
                btnConnect.Enabled = false;

                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIP, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;
                
                lblStatus.Text = "Logowanie...";
                lblStatus.ForeColor = Color.Blue;

                // Wyślij żądanie logowania lub rejestracji
                if (isRegister)
                {
                    var registerData = new
                    {
                        MessageType = "register",
                        Nick = CurrentNick,
                        Password = currentPassword
                    };
                    await SendToServer(registerData);
                }
                else
                {
                    var loginData = new
                    {
                        MessageType = "login",
                        Nick = CurrentNick,
                        Password = currentPassword
                    };
                    await SendToServer(loginData);
                }

                // Rozpocznij odbieranie wiadomości
                _ = Task.Run(() => ReceiveMessages());

                btnConnect.Text = "Rozłącz";
                btnConnect.Enabled = true;

                AddMessageToChat("System", $"Próba {(isRegister ? "rejestracji" : "logowania")} do serwera {serverIP}:{serverPort}", DateTime.Now, Color.Blue);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Błąd połączenia";
                lblStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
                MessageBox.Show($"Nie można połączyć się z serwerem: {ex.Message}", "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAccountSettings_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Musisz być połączony z serwerem!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (activeAccountForm == null || activeAccountForm.IsDisposed)
            {
                activeAccountForm = new AccountSettingsForm(this);
                activeAccountForm.Show();
            }
            else
            {
                activeAccountForm.BringToFront();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isConnected)
            {
                Disconnect();
            }
        }

        // === NOWE FUNKCJE DLA SYSTEMU LOGOWANIA ===

        private void HandleLoginResponse(dynamic response)
        {
            this.Invoke(new Action(() =>
            {
                if (response == null)
                {
                    AddMessageToChat("System", "Błąd: Otrzymano pustą odpowiedź z serwera", DateTime.Now, Color.Red);
                    Disconnect();
                    return;
                }

                bool success = response.Success;
                string message = response.Message?.ToString();

                if (success)
                {
                    currentUserId = response.UserId?.ToString();
                    Console.WriteLine($"[DEBUG] HandleLoginResponse - currentUserId ustawione na: '{currentUserId}'");
                    lblStatus.Text = "Połączony";
                    lblStatus.ForeColor = Color.Green;
                    txtMessage.Enabled = true;
                    btnSend.Enabled = true;
                    btnEmoji.Enabled = true;
                    btnCreateChannel.Enabled = true;
                    btnJoinChannel.Enabled = true;
                    txtMessage.Focus();

                    // Upewnij się, że użytkownik jest w globalnym kanale
                    currentChannelId = "global";
                    lblCurrentChannel.Text = "🌍 Globalny";

                    AddMessageToChat("System", $"Pomyślnie zalogowano: {message}", DateTime.Now, Color.Green);
                    
                    // Rozpocznij auto-reconnect timer
                    if (chkAutoReconnect.Checked)
                    {
                        reconnectTimer.Start();
                    }
                    
                    // Poproś o listę użytkowników online
                    _ = Task.Run(async () => await RequestOnlineUsers());
                }
                else
                {
                    lblStatus.Text = "Błąd logowania";
                    lblStatus.ForeColor = Color.Red;
                    AddMessageToChat("System", $"Błąd logowania: {message}", DateTime.Now, Color.Red);
                    
                    // Rozłącz po błędzie logowania
                    Disconnect();
                }
            }));
        }

        private void HandleRegisterResponse(dynamic response)
        {
            this.Invoke(new Action(() =>
            {
                if (response == null)
                {
                    AddMessageToChat("System", "Błąd: Otrzymano pustą odpowiedź z serwera", DateTime.Now, Color.Red);
                    Disconnect();
                    return;
                }

                bool success = response.Success;
                string message = response.Message?.ToString();

                if (success)
                {
                    currentUserId = response.UserId?.ToString();
                    Console.WriteLine($"[DEBUG] HandleRegisterResponse - currentUserId ustawione na: '{currentUserId}'");
                    lblStatus.Text = "Połączony";
                    lblStatus.ForeColor = Color.Green;
                    txtMessage.Enabled = true;
                    btnSend.Enabled = true;
                    btnEmoji.Enabled = true;
                    btnCreateChannel.Enabled = true;
                    btnJoinChannel.Enabled = true;
                    txtMessage.Focus();

                    // Upewnij się, że użytkownik jest w globalnym kanale
                    currentChannelId = "global";
                    lblCurrentChannel.Text = "🌍 Globalny";

                    AddMessageToChat("System", $"Pomyślnie zarejestrowano: {message}", DateTime.Now, Color.Green);
                    
                    // Rozpocznij auto-reconnect timer
                    if (chkAutoReconnect.Checked)
                    {
                        reconnectTimer.Start();
                    }
                    
                    // Poproś o listę użytkowników online
                    _ = Task.Run(async () => await RequestOnlineUsers());
                }
                else
                {
                    lblStatus.Text = "Błąd rejestracji";
                    lblStatus.ForeColor = Color.Red;
                    AddMessageToChat("System", $"Błąd rejestracji: {message}", DateTime.Now, Color.Red);
                    
                    // Rozłącz po błędzie rejestracji
                    Disconnect();
                }
            }));
        }

        private void HandleOwnedChannelsResponse(dynamic channelsData)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    if (channelsData == null)
                    {
                        AddMessageToChat("System", "Błąd: Otrzymano pustą listę kanałów z serwera", DateTime.Now, Color.Red);
                        return;
                    }

                    var ownedChannelsList = JsonConvert.DeserializeObject<List<ChannelInfo>>(channelsData.ToString());
                    
                    // Zaktualizuj AccountSettingsForm jeśli jest otwarte
                    if (activeAccountForm != null && !activeAccountForm.IsDisposed)
                    {
                        activeAccountForm.UpdateOwnedChannels(ownedChannelsList);
                    }
                }
                catch (Exception ex)
                {
                    AddMessageToChat("System", $"Błąd ładowania listy kanałów: {ex.Message}", DateTime.Now, Color.Red);
                }
            }));
        }

        public async Task ChangeNick(string newNick)
        {
            try
            {
                var data = new
                {
                    MessageType = "change_nick",
                    NewNick = newNick
                };
                await SendToServer(data);
                
                // Lokalnie zaktualizuj nick
                CurrentNick = newNick;
                lblNick.Text = $"Zalogowany jako: {CurrentNick}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Błąd komunikacji z serwerem: {ex.Message}");
            }
        }

        public async Task ChangePassword(string currentPassword, string newPassword)
        {
            try
            {
                var data = new
                {
                    MessageType = "change_password",
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };
                await SendToServer(data);
                
                // Lokalnie zaktualizuj hasło
                this.currentPassword = newPassword;
            }
            catch (Exception ex)
            {
                throw new Exception($"Błąd komunikacji z serwerem: {ex.Message}");
            }
        }

        public async Task DeleteChannel(string channelId)
        {
            try
            {
                var data = new
                {
                    MessageType = "delete_channel",
                    ChannelId = channelId
                };
                await SendToServer(data);
            }
            catch (Exception ex)
            {
                throw new Exception($"Błąd komunikacji z serwerem: {ex.Message}");
            }
        }

        public async Task<List<ChannelInfo>> GetOwnedChannels()
        {
            try
            {
                var data = new
                {
                    MessageType = "get_owned_channels"
                };
                await SendToServer(data);
                
                // Zwróć lokalną listę kanałów które użytkownik może zarządzać
                // Serwer wyśle aktualizację przez owned_channels_response
                await Task.Delay(100); // Krótkie opóźnienie na odpowiedź
                return channels.Where(c => c.OwnerId == currentUserId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Błąd komunikacji z serwerem: {ex.Message}");
            }
        }

        public void ShowChannelSettings(string channelId)
        {
            var channel = channels.FirstOrDefault(c => c.Id == channelId);
            if (channel != null && (activeSettingsForm == null || activeSettingsForm.IsDisposed))
            {
                activeSettingsForm = new ChannelSettingsForm(channel, this);
                activeSettingsForm.Show();
            }
            else if (activeSettingsForm != null)
            {
                activeSettingsForm.BringToFront();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized) return;

            // Dostosuj rozmiary kontrolek do nowego rozmiaru okna
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            // Minimalne wymiary, żeby uniknąć nakładania
            if (formWidth < 800) formWidth = 800;
            if (formHeight < 600) formHeight = 600;

            // Panel kanałów - stała szerokość, pełna wysokość
            if (pnlChannels != null)
            {
                pnlChannels.Height = formHeight - 60;
            }

            // Chat - dostosuj szerokość i wysokość (zostaw więcej miejsca)
            if (rtbChat != null)
            {
                int chatWidth = formWidth - 420; // 200 (panel kanałów) + 200 (panel użytkowników) + 20 (marginesy)
                if (chatWidth < 300) chatWidth = 300; // Minimalna szerokość
                rtbChat.Width = chatWidth;
                rtbChat.Height = formHeight - 180; // Więcej miejsca na pole wiadomości
            }

            // Pole wiadomości - dostosuj szerokość i pozycję
            if (txtMessage != null)
            {
                int msgWidth = formWidth - 520; // Zostaw miejsce na przyciski
                if (msgWidth < 200) msgWidth = 200; // Minimalna szerokość
                txtMessage.Width = msgWidth;
                txtMessage.Location = new Point(210, formHeight - 120); // Więcej miejsca od dołu
            }

            // Przyciski wysyłania - przesuń w prawo i w dół
            if (btnSend != null)
            {
                btnSend.Location = new Point(formWidth - 300, formHeight - 120);
            }
            if (btnEmoji != null)
            {
                btnEmoji.Location = new Point(formWidth - 200, formHeight - 120);
            }

            // Panel użytkowników online - przesuń w prawo
            if (lblOnlineUsers != null)
            {
                lblOnlineUsers.Location = new Point(formWidth - 190, 55);
            }
            if (lstOnlineUsers != null)
            {
                lstOnlineUsers.Location = new Point(formWidth - 190, 95);
                int listHeight = formHeight - 250; // Zostaw miejsce na status
                if (listHeight < 100) listHeight = 100;
                lstOnlineUsers.Height = listHeight;
            }

            // Status - przesuń w prawo i w dół
            if (lblStatus != null)
            {
                lblStatus.Location = new Point(formWidth - 190, formHeight - 120);
            }
        }
    }
}