using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CzatujiemyClient
{
    public partial class ChannelSettingsForm : Form
    {
        private ChannelInfo channel;
        private TabControl tabControl;
        private MainForm parentForm;
        private List<ChannelMember> channelMembers = new List<ChannelMember>();
        

        private TextBox txtName;
        private ComboBox cbIcon;
        private TextBox txtPassword;
        private CheckBox chkHasPassword;
        

        private ListBox lstMembers;
        private Button btnKick;
        private Button btnBan;
        private Button btnMute;

        public ChannelSettingsForm(ChannelInfo channelInfo, MainForm parent)
        {
            this.channel = channelInfo;
            this.parentForm = parent;
            InitializeComponent();
            LoadChannelData();
            LoadChannelMembers();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 500);
            this.Text = $"Ustawienia kanału: {channel.Name}";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            tabControl = new TabControl();
            tabControl.Location = new Point(10, 10);
            tabControl.Size = new Size(570, 430);
            this.Controls.Add(tabControl);

    
            var tabGeneral = new TabPage("Ogólne");
            tabControl.TabPages.Add(tabGeneral);


            var lblName = new Label();
            lblName.Text = "Nazwa kanału:";
            lblName.Location = new Point(20, 20);
            lblName.Size = new Size(100, 20);
            tabGeneral.Controls.Add(lblName);

            txtName = new TextBox();
            txtName.Location = new Point(130, 18);
            txtName.Size = new Size(200, 25);
            txtName.MaxLength = 30;
            tabGeneral.Controls.Add(txtName);


            var lblIcon = new Label();
            lblIcon.Text = "Ikona:";
            lblIcon.Location = new Point(20, 55);
            lblIcon.Size = new Size(100, 20);
            tabGeneral.Controls.Add(lblIcon);

            cbIcon = new ComboBox();
            cbIcon.Location = new Point(130, 53);
            cbIcon.Size = new Size(100, 25);
            cbIcon.DropDownStyle = ComboBoxStyle.DropDownList;
            cbIcon.Items.AddRange(new string[] { "💬", "🎮", "📚", "🎵", "⚽", "🍕", "💻", "🎨", "🌟", "🔥", "💡", "🚀" });
            tabGeneral.Controls.Add(cbIcon);


            chkHasPassword = new CheckBox();
            chkHasPassword.Text = "Kanał chroniony hasłem";
            chkHasPassword.Location = new Point(20, 90);
            chkHasPassword.Size = new Size(200, 25);
            chkHasPassword.CheckedChanged += ChkHasPassword_CheckedChanged;
            tabGeneral.Controls.Add(chkHasPassword);

            var lblPassword = new Label();
            lblPassword.Text = "Hasło:";
            lblPassword.Location = new Point(20, 125);
            lblPassword.Size = new Size(100, 20);
            tabGeneral.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(130, 123);
            txtPassword.Size = new Size(200, 25);
            txtPassword.MaxLength = 50;
            txtPassword.UseSystemPasswordChar = true;
            tabGeneral.Controls.Add(txtPassword);


            var btnSave = new Button();
            btnSave.Text = "Zapisz zmiany";
            btnSave.Location = new Point(130, 170);
            btnSave.Size = new Size(120, 35);
            btnSave.Click += BtnSave_Click;
            tabGeneral.Controls.Add(btnSave);

    
            var tabMembers = new TabPage("Członkowie");
            tabControl.TabPages.Add(tabMembers);

            var lblMembers = new Label();
            lblMembers.Text = "Lista członków:";
            lblMembers.Location = new Point(20, 20);
            lblMembers.Size = new Size(150, 20);
            tabMembers.Controls.Add(lblMembers);

            lstMembers = new ListBox();
            lstMembers.Location = new Point(20, 45);
            lstMembers.Size = new Size(350, 250);
            lstMembers.DrawMode = DrawMode.OwnerDrawFixed;
            lstMembers.ItemHeight = 25;
            lstMembers.DrawItem += LstMembers_DrawItem;
            lstMembers.SelectedIndexChanged += LstMembers_SelectedIndexChanged;
            tabMembers.Controls.Add(lstMembers);


            btnKick = new Button();
            btnKick.Text = "👢 Wykop";
            btnKick.Location = new Point(400, 45);
            btnKick.Size = new Size(95, 35);
            btnKick.BackColor = Color.FromArgb(230, 126, 34);
            btnKick.ForeColor = Color.White;
            btnKick.FlatStyle = FlatStyle.Flat;
            btnKick.FlatAppearance.BorderSize = 0;
            btnKick.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnKick.Enabled = false;
            btnKick.Click += BtnKick_Click;
            tabMembers.Controls.Add(btnKick);

            btnBan = new Button();
            btnBan.Text = "🚫 Zbanuj";
            btnBan.Location = new Point(400, 85);
            btnBan.Size = new Size(95, 35);
            btnBan.BackColor = Color.FromArgb(231, 76, 60);
            btnBan.ForeColor = Color.White;
            btnBan.FlatStyle = FlatStyle.Flat;
            btnBan.FlatAppearance.BorderSize = 0;
            btnBan.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBan.Enabled = false;
            btnBan.Click += BtnBan_Click;
            tabMembers.Controls.Add(btnBan);

            btnMute = new Button();
            btnMute.Text = "🔇 Wycisz";
            btnMute.Location = new Point(400, 125);
            btnMute.Size = new Size(95, 35);
            btnMute.BackColor = Color.FromArgb(243, 156, 18);
            btnMute.ForeColor = Color.White;
            btnMute.FlatStyle = FlatStyle.Flat;
            btnMute.FlatAppearance.BorderSize = 0;
            btnMute.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnMute.Enabled = false;
            btnMute.Click += BtnMute_Click;
            tabMembers.Controls.Add(btnMute);


            var btnClose = new Button();
            btnClose.Text = "Zamknij";
            btnClose.Location = new Point(500, 450);
            btnClose.Size = new Size(80, 35);
            btnClose.DialogResult = DialogResult.OK;
            this.Controls.Add(btnClose);
        }

        private void LoadChannelData()
        {
            txtName.Text = channel.Name;
            chkHasPassword.Checked = channel.HasPassword;
            

            for (int i = 0; i < cbIcon.Items.Count; i++)
            {
                if (cbIcon.Items[i].ToString() == channel.Icon)
                {
                    cbIcon.SelectedIndex = i;
                    break;
                }
            }
            if (cbIcon.SelectedIndex == -1) cbIcon.SelectedIndex = 0;

            ChkHasPassword_CheckedChanged(null, null);
        }

        private void ChkHasPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = chkHasPassword.Checked;
            if (!chkHasPassword.Checked)
            {
                txtPassword.Clear();
            }
        }

        private void LstMembers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= channelMembers.Count) return;

            var member = channelMembers[e.Index];
            e.DrawBackground();


            var brush = new SolidBrush(e.ForeColor);
            if (member.IsBanned) brush.Color = Color.Red;
            else if (member.IsMuted) brush.Color = Color.Orange;
            else if (!member.IsActive) brush.Color = Color.Gray;


            string statusIcon = member.IsActive ? "🟢" : "⚫";
            if (member.IsBanned) statusIcon = "🚫";
            else if (member.IsMuted) statusIcon = "🔇";

            string roleIcon = "";
            switch (member.Role)
            {
                case "Owner": roleIcon = "👑"; break;
                case "Admin": roleIcon = "⭐"; break;
                case "Moderator": roleIcon = "🛡️"; break;
                default: roleIcon = "👤"; break;
            }

            string displayText = $"{statusIcon} {roleIcon} {member.Nick}";
            if (!member.IsActive && member.LastSeen != DateTime.MinValue)
            {
                displayText += $" (ostatnio: {member.LastSeen:dd.MM HH:mm})";
            }

            e.Graphics.DrawString(displayText, e.Font, brush, e.Bounds.Location);
            e.DrawFocusRectangle();
        }

        private void LstMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = lstMembers.SelectedIndex >= 0;
            btnKick.Enabled = hasSelection;
            btnBan.Enabled = hasSelection;
            btnMute.Enabled = hasSelection;

            if (hasSelection && lstMembers.SelectedIndex < channelMembers.Count)
            {
                var selectedMember = channelMembers[lstMembers.SelectedIndex];
                

                btnBan.Text = selectedMember.IsBanned ? "🔓 Odbanuj" : "🚫 Zbanuj";
                

                btnMute.Text = selectedMember.IsMuted ? "🔊 Odcisz" : "🔇 Wycisz";
            }
            else
            {

                btnBan.Text = "🚫 Zbanuj";
                btnMute.Text = "🔇 Wycisz";
            }
        }

        private async void LoadChannelMembers()
        {
            if (parentForm != null)
            {
                await parentForm.RequestChannelMembers(channel.Id);
            }
        }

        public void UpdateChannelMembers(List<ChannelMember> members)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateChannelMembers(members)));
                return;
            }

            channelMembers = members;
            lstMembers.Items.Clear();
            foreach (var member in channelMembers)
            {
                lstMembers.Items.Add(member);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Wprowadź nazwę kanału!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (parentForm != null)
            {
                await parentForm.UpdateChannel(channel.Id, txtName.Text, cbIcon.SelectedItem?.ToString(), 
                    chkHasPassword.Checked ? txtPassword.Text : null);
            }
        }

        private async void BtnKick_Click(object sender, EventArgs e)
        {
            if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < channelMembers.Count)
            {
                var selectedMember = channelMembers[lstMembers.SelectedIndex];
                if (MessageBox.Show($"Czy na pewno chcesz wykopać {selectedMember.Nick}?", "Potwierdzenie", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (parentForm != null)
                    {
                        await parentForm.KickUser(channel.Id, selectedMember.UserId);
                    }
                }
            }
        }

        private async void BtnBan_Click(object sender, EventArgs e)
        {
            if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < channelMembers.Count)
            {
                var selectedMember = channelMembers[lstMembers.SelectedIndex];
                string action = selectedMember.IsBanned ? "odbanować" : "zbanować";
                if (MessageBox.Show($"Czy na pewno chcesz {action} {selectedMember.Nick}?", "Potwierdzenie", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (parentForm != null)
                    {
                        if (selectedMember.IsBanned)
                            await parentForm.UnbanUser(channel.Id, selectedMember.UserId);
                        else
                            await parentForm.BanUser(channel.Id, selectedMember.UserId);
                        
                        // Odśwież listę członków po akcji
                        await Task.Delay(500); // Krótkie opóźnienie na przetworzenie przez serwer
                        LoadChannelMembers();
                    }
                }
            }
        }

        private async void BtnMute_Click(object sender, EventArgs e)
        {
            if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < channelMembers.Count)
            {
                var selectedMember = channelMembers[lstMembers.SelectedIndex];
                string action = selectedMember.IsMuted ? "odciszyć" : "wyciszyć";
                if (MessageBox.Show($"Czy na pewno chcesz {action} {selectedMember.Nick}?", "Potwierdzenie", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (parentForm != null)
                    {
                        if (selectedMember.IsMuted)
                            await parentForm.UnmuteUser(channel.Id, selectedMember.UserId);
                        else
                            await parentForm.MuteUser(channel.Id, selectedMember.UserId);
                        
                        // Odśwież listę członków po akcji
                        await Task.Delay(500); // Krótkie opóźnienie na przetworzenie przez serwer
                        LoadChannelMembers();
                    }
                }
            }
        }
    }
}