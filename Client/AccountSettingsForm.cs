using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CzatujiemyClient
{
    public partial class AccountSettingsForm : Form
    {
        private MainForm parentForm;
        private string currentNick;
        private List<ChannelInfo> ownedChannels = new List<ChannelInfo>();


        private TabControl tabControl;
        private TextBox txtCurrentNick;
        private TextBox txtNewNick;
        private TextBox txtCurrentPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private ListBox lstOwnedChannels;
        private Button btnChangeNick;
        private Button btnChangePassword;
        private Button btnDeleteChannel;
        private Button btnChannelSettings;
        private Button btnClose;
        private Label lblChannelInfo;
        private CheckBox chkShowPasswords;

        public AccountSettingsForm(MainForm parent)
        {
            this.parentForm = parent;
            this.currentNick = parent.CurrentNick;
            InitializeComponent();
            _ = LoadOwnedChannels();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(550, 450);
            this.Text = "Ustawienia konta";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 248, 255);


            var lblTitle = new Label();
            lblTitle.Text = $"⚙️ Ustawienia konta: {currentNick}";
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(500, 30);
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = Color.DarkBlue;
            this.Controls.Add(lblTitle);


            tabControl = new TabControl();
            tabControl.Location = new Point(20, 50);
            tabControl.Size = new Size(500, 320);
            tabControl.Font = new Font("Segoe UI", 9);
            this.Controls.Add(tabControl);


            var tabProfile = new TabPage("Profil");
            tabProfile.BackColor = Color.FromArgb(250, 250, 250);
            tabControl.TabPages.Add(tabProfile);


            var lblCurrentNick = new Label();
            lblCurrentNick.Text = "Obecny nick:";
            lblCurrentNick.Location = new Point(20, 20);
            lblCurrentNick.Size = new Size(100, 20);
            lblCurrentNick.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            tabProfile.Controls.Add(lblCurrentNick);

            txtCurrentNick = new TextBox();
            txtCurrentNick.Location = new Point(130, 17);
            txtCurrentNick.Size = new Size(200, 25);
            txtCurrentNick.Text = currentNick;
            txtCurrentNick.ReadOnly = true;
            txtCurrentNick.BackColor = Color.LightGray;
            tabProfile.Controls.Add(txtCurrentNick);


            var lblNewNick = new Label();
            lblNewNick.Text = "Nowy nick:";
            lblNewNick.Location = new Point(20, 55);
            lblNewNick.Size = new Size(100, 20);
            tabProfile.Controls.Add(lblNewNick);

            txtNewNick = new TextBox();
            txtNewNick.Location = new Point(130, 52);
            txtNewNick.Size = new Size(200, 25);
            txtNewNick.MaxLength = 20;
            tabProfile.Controls.Add(txtNewNick);

            btnChangeNick = new Button();
            btnChangeNick.Text = "Zmień nick";
            btnChangeNick.Location = new Point(340, 50);
            btnChangeNick.Size = new Size(100, 30);
            btnChangeNick.BackColor = Color.DodgerBlue;
            btnChangeNick.ForeColor = Color.White;
            btnChangeNick.FlatStyle = FlatStyle.Flat;
            btnChangeNick.Click += BtnChangeNick_Click;
            tabProfile.Controls.Add(btnChangeNick);


            var separator1 = new Label();
            separator1.BorderStyle = BorderStyle.Fixed3D;
            separator1.Location = new Point(20, 95);
            separator1.Size = new Size(420, 2);
            tabProfile.Controls.Add(separator1);


            var lblPasswordSection = new Label();
            lblPasswordSection.Text = "Zmiana hasła:";
            lblPasswordSection.Location = new Point(20, 110);
            lblPasswordSection.Size = new Size(200, 20);
            lblPasswordSection.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            tabProfile.Controls.Add(lblPasswordSection);

            var lblCurrentPassword = new Label();
            lblCurrentPassword.Text = "Obecne hasło:";
            lblCurrentPassword.Location = new Point(20, 135);
            lblCurrentPassword.Size = new Size(100, 20);
            tabProfile.Controls.Add(lblCurrentPassword);

            txtCurrentPassword = new TextBox();
            txtCurrentPassword.Location = new Point(130, 132);
            txtCurrentPassword.Size = new Size(200, 25);
            txtCurrentPassword.UseSystemPasswordChar = true;
            txtCurrentPassword.MaxLength = 50;
            tabProfile.Controls.Add(txtCurrentPassword);

            var lblNewPassword = new Label();
            lblNewPassword.Text = "Nowe hasło:";
            lblNewPassword.Location = new Point(20, 165);
            lblNewPassword.Size = new Size(100, 20);
            tabProfile.Controls.Add(lblNewPassword);

            txtNewPassword = new TextBox();
            txtNewPassword.Location = new Point(130, 162);
            txtNewPassword.Size = new Size(200, 25);
            txtNewPassword.UseSystemPasswordChar = true;
            txtNewPassword.MaxLength = 50;
            tabProfile.Controls.Add(txtNewPassword);

            var lblConfirmPassword = new Label();
            lblConfirmPassword.Text = "Potwierdź hasło:";
            lblConfirmPassword.Location = new Point(20, 195);
            lblConfirmPassword.Size = new Size(100, 20);
            tabProfile.Controls.Add(lblConfirmPassword);

            txtConfirmPassword = new TextBox();
            txtConfirmPassword.Location = new Point(130, 192);
            txtConfirmPassword.Size = new Size(200, 25);
            txtConfirmPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.MaxLength = 50;
            tabProfile.Controls.Add(txtConfirmPassword);

            chkShowPasswords = new CheckBox();
            chkShowPasswords.Text = "Pokaż hasła";
            chkShowPasswords.Location = new Point(340, 165);
            chkShowPasswords.Size = new Size(100, 20);
            chkShowPasswords.CheckedChanged += ChkShowPasswords_CheckedChanged;
            tabProfile.Controls.Add(chkShowPasswords);

            btnChangePassword = new Button();
            btnChangePassword.Text = "Zmień hasło";
            btnChangePassword.Location = new Point(340, 190);
            btnChangePassword.Size = new Size(100, 30);
            btnChangePassword.BackColor = Color.ForestGreen;
            btnChangePassword.ForeColor = Color.White;
            btnChangePassword.FlatStyle = FlatStyle.Flat;
            btnChangePassword.Click += BtnChangePassword_Click;
            tabProfile.Controls.Add(btnChangePassword);


            var tabChannels = new TabPage("Moje kanały(nie dziala xd)");
            tabChannels.BackColor = Color.FromArgb(250, 250, 250);
            tabControl.TabPages.Add(tabChannels);

            var lblMyChannels = new Label();
            lblMyChannels.Text = "tak. jestem zbyt leniwy zeby to dzialalo XD";
            lblMyChannels.Location = new Point(20, 20);
            lblMyChannels.Size = new Size(200, 20);
            lblMyChannels.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            tabChannels.Controls.Add(lblMyChannels);

            lstOwnedChannels = new ListBox();
            lstOwnedChannels.Location = new Point(20, 45);
            lstOwnedChannels.Size = new Size(300, 200);
            lstOwnedChannels.DrawMode = DrawMode.OwnerDrawFixed;
            lstOwnedChannels.ItemHeight = 30;
            lstOwnedChannels.DrawItem += LstOwnedChannels_DrawItem;
            lstOwnedChannels.SelectedIndexChanged += LstOwnedChannels_SelectedIndexChanged;
            tabChannels.Controls.Add(lstOwnedChannels);

            btnChannelSettings = new Button();
            btnChannelSettings.Text = "Ustawienia kanału";
            btnChannelSettings.Location = new Point(340, 45);
            btnChannelSettings.Size = new Size(120, 35);
            btnChannelSettings.BackColor = Color.DarkOrange;
            btnChannelSettings.ForeColor = Color.White;
            btnChannelSettings.FlatStyle = FlatStyle.Flat;
            btnChannelSettings.Enabled = false;
            btnChannelSettings.Click += BtnChannelSettings_Click;
            tabChannels.Controls.Add(btnChannelSettings);

            btnDeleteChannel = new Button();
            btnDeleteChannel.Text = "🗑️ Usuń kanał";
            btnDeleteChannel.Location = new Point(340, 90);
            btnDeleteChannel.Size = new Size(120, 35);
            btnDeleteChannel.BackColor = Color.Crimson;
            btnDeleteChannel.ForeColor = Color.White;
            btnDeleteChannel.FlatStyle = FlatStyle.Flat;
            btnDeleteChannel.Enabled = false;
            btnDeleteChannel.Click += BtnDeleteChannel_Click;
            tabChannels.Controls.Add(btnDeleteChannel);

            lblChannelInfo = new Label();
            lblChannelInfo.Text = "Wybierz kanał, aby wyświetlić szczegóły.";
            lblChannelInfo.Location = new Point(20, 255);
            lblChannelInfo.Size = new Size(420, 20);
            lblChannelInfo.ForeColor = Color.Gray;
            lblChannelInfo.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            tabChannels.Controls.Add(lblChannelInfo);


            btnClose = new Button();
            btnClose.Text = "Zamknij";
            btnClose.Location = new Point(440, 385);
            btnClose.Size = new Size(80, 35);
            btnClose.BackColor = Color.Gray;
            btnClose.ForeColor = Color.White;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Click += BtnClose_Click;
            this.Controls.Add(btnClose);

            this.CancelButton = btnClose;
        }

        private void ChkShowPasswords_CheckedChanged(object sender, EventArgs e)
        {
            txtCurrentPassword.UseSystemPasswordChar = !chkShowPasswords.Checked;
            txtNewPassword.UseSystemPasswordChar = !chkShowPasswords.Checked;
            txtConfirmPassword.UseSystemPasswordChar = !chkShowPasswords.Checked;
        }

        private async void BtnChangeNick_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewNick.Text))
            {
                MessageBox.Show("Wprowadź nowy nick!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewNick.Text.Length < 3)
            {
                MessageBox.Show("Nick musi mieć co najmniej 3 znaki!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewNick.Text == currentNick)
            {
                MessageBox.Show("Nowy nick jest taki sam jak obecny!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Czy na pewno chcesz zmienić nick z '{currentNick}' na '{txtNewNick.Text}'?",
                "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                btnChangeNick.Enabled = false;
                try
                {
                    await parentForm.ChangeNick(txtNewNick.Text);
                    MessageBox.Show("Nick został zmieniony!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    currentNick = txtNewNick.Text;
                    txtCurrentNick.Text = currentNick;
                    txtNewNick.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd przy zmianie nicku: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnChangeNick.Enabled = true;
                }
            }
        }

        private async void BtnChangePassword_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCurrentPassword.Text))
            {
                MessageBox.Show("Wprowadź obecne hasło!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewPassword.Text))
            {
                MessageBox.Show("Wprowadź nowe hasło!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewPassword.Text.Length < 6)
            {
                MessageBox.Show("Nowe hasło musi mieć co najmniej 6 znaków!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Nowe hasło i potwierdzenie nie są identyczne!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Czy na pewno chcesz zmienić hasło?", "Potwierdzenie", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                btnChangePassword.Enabled = false;
                try
                {
                    await parentForm.ChangePassword(txtCurrentPassword.Text, txtNewPassword.Text);
                    MessageBox.Show("Hasło zostało zmienione!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtCurrentPassword.Clear();
                    txtNewPassword.Clear();
                    txtConfirmPassword.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd przy zmianie hasła: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnChangePassword.Enabled = true;
                }
            }
        }

        private async void BtnDeleteChannel_Click(object sender, EventArgs e)
        {
            if (lstOwnedChannels.SelectedItem == null)
                return;

            var channel = (ChannelInfo)lstOwnedChannels.SelectedItem;
            
            var result = MessageBox.Show($"Czy na pewno chcesz usunąć kanał '{channel.Name}'?\n\n" +
                "UWAGA: Ta operacja jest nieodwracalna!\nWszyscy użytkownicy zostaną wyrzuceni z kanału, a cała historia zostanie utracona!",
                "Potwierdzenie usunięcia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                btnDeleteChannel.Enabled = false;
                try
                {
                    await parentForm.DeleteChannel(channel.Id);
                    MessageBox.Show($"Kanał '{channel.Name}' został usunięty!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadOwnedChannels();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd przy usuwaniu kanału: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnDeleteChannel.Enabled = true;
                }
            }
        }

        private void BtnChannelSettings_Click(object sender, EventArgs e)
        {
            if (lstOwnedChannels.SelectedItem != null)
            {
                var channel = (ChannelInfo)lstOwnedChannels.SelectedItem;
                parentForm.ShowChannelSettings(channel.Id);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async Task LoadOwnedChannels()
        {
            try
            {
                ownedChannels = await parentForm.GetOwnedChannels();
                RefreshChannelsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania kanałów: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateOwnedChannels(List<ChannelInfo> channels)
        {
            ownedChannels = channels;
            RefreshChannelsList();
        }

        private void RefreshChannelsList()
        {
            lstOwnedChannels.Items.Clear();
            foreach (var channel in ownedChannels)
            {
                lstOwnedChannels.Items.Add(channel);
            }
        }

        private void LstOwnedChannels_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var channel = (ChannelInfo)lstOwnedChannels.Items[e.Index];
            e.DrawBackground();

            var font = new Font("Segoe UI", 9, FontStyle.Bold);
            var brush = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? Color.White : Color.Black);

            string text = $"{channel.Icon} {channel.Name} ({channel.MemberCount} użytkowników)";
            e.Graphics.DrawString(text, font, brush, e.Bounds.X + 5, e.Bounds.Y + 5);

            e.DrawFocusRectangle();
            font.Dispose();
            brush.Dispose();
        }

        private void LstOwnedChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = lstOwnedChannels.SelectedItem != null;
            btnDeleteChannel.Enabled = hasSelection;
            btnChannelSettings.Enabled = hasSelection;

            if (hasSelection)
            {
                var channel = (ChannelInfo)lstOwnedChannels.SelectedItem;
                lblChannelInfo.Text = $"Kanał: {channel.Name} | Utworzony: {DateTime.Now.ToShortDateString()} | Członkowie: {channel.MemberCount}";
            }
            else
            {
                lblChannelInfo.Text = "Wybierz kanał, aby wyświetlić szczegóły.";
            }
        }
    }
}