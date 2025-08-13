using System;
using System.Drawing;
using System.Windows.Forms;

namespace CzatujiemyClient
{
    public partial class LoginForm : Form
    {
        public string Nick { get; private set; }
        public string Password { get; private set; }
        public string ServerIP { get; private set; }
        public int ServerPort { get; private set; }
        public bool IsRegister { get; private set; }

        private TextBox txtNick;
        private TextBox txtPassword;
        private TextBox txtServerIP;
        private NumericUpDown nudPort;
        private ComboBox cbServerPresets;
        private Button btnLogin;
        private Button btnRegister;
        private Button btnCancel;
        private CheckBox chkShowPassword;
        private CheckBox chkRememberServer;

        public LoginForm()
        {
            InitializeComponent();
            LoadServerSettings();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(420, 380);
            this.Text = "Czatujemy 2.0 - Logowanie";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 248, 255);


            var lblTitle = new Label();
            lblTitle.Text = "🗨️ Czatujemy 2.0";
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(380, 35);
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.ForeColor = Color.DarkBlue;
            this.Controls.Add(lblTitle);


            var lblServer = new Label();
            lblServer.Text = "Serwer:";
            lblServer.Location = new Point(20, 65);
            lblServer.Size = new Size(60, 20);
            lblServer.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.Controls.Add(lblServer);

            cbServerPresets = new ComboBox();
            cbServerPresets.Location = new Point(20, 85);
            cbServerPresets.Size = new Size(360, 25);
            cbServerPresets.DropDownStyle = ComboBoxStyle.DropDownList;
            cbServerPresets.Items.AddRange(new string[] {
                "Serwer lokalny (127.0.0.1:8080)",
                "Serwer publiczny (9.223.113.121:8080)",
                "Własny adres IP..."
            });
            cbServerPresets.SelectedIndex = 1;
            cbServerPresets.SelectedIndexChanged += CbServerPresets_SelectedIndexChanged;
            this.Controls.Add(cbServerPresets);


            var pnlCustomServer = new Panel();
            pnlCustomServer.Location = new Point(20, 115);
            pnlCustomServer.Size = new Size(360, 30);
            pnlCustomServer.Visible = false;
            this.Controls.Add(pnlCustomServer);

            var lblIP = new Label();
            lblIP.Text = "IP:";
            lblIP.Location = new Point(0, 8);
            lblIP.Size = new Size(25, 20);
            pnlCustomServer.Controls.Add(lblIP);

            txtServerIP = new TextBox();
            txtServerIP.Location = new Point(30, 5);
            txtServerIP.Size = new Size(200, 25);
            txtServerIP.Text = "127.0.0.1";
            pnlCustomServer.Controls.Add(txtServerIP);

            var lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.Location = new Point(240, 8);
            lblPort.Size = new Size(35, 20);
            pnlCustomServer.Controls.Add(lblPort);

            nudPort = new NumericUpDown();
            nudPort.Location = new Point(280, 5);
            nudPort.Size = new Size(80, 25);
            nudPort.Minimum = 1;
            nudPort.Maximum = 65535;
            nudPort.Value = 8080;
            pnlCustomServer.Controls.Add(nudPort);

            cbServerPresets.Tag = pnlCustomServer;


            var lblLoginData = new Label();
            lblLoginData.Text = "Dane logowania:";
            lblLoginData.Location = new Point(20, 160);
            lblLoginData.Size = new Size(120, 20);
            lblLoginData.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.Controls.Add(lblLoginData);

            var lblNick = new Label();
            lblNick.Text = "Nick:";
            lblNick.Location = new Point(20, 185);
            lblNick.Size = new Size(40, 20);
            this.Controls.Add(lblNick);

            txtNick = new TextBox();
            txtNick.Location = new Point(70, 182);
            txtNick.Size = new Size(310, 25);
            txtNick.MaxLength = 20;
            this.Controls.Add(txtNick);

            var lblPassword = new Label();
            lblPassword.Text = "Hasło:";
            lblPassword.Location = new Point(20, 215);
            lblPassword.Size = new Size(50, 20);
            this.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(70, 212);
            txtPassword.Size = new Size(250, 25);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.MaxLength = 50;
            this.Controls.Add(txtPassword);

            chkShowPassword = new CheckBox();
            chkShowPassword.Text = "Pokaż";
            chkShowPassword.Location = new Point(330, 215);
            chkShowPassword.Size = new Size(65, 20);
            chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;
            this.Controls.Add(chkShowPassword);


            chkRememberServer = new CheckBox();
            chkRememberServer.Text = "Zapamiętaj ustawienia serwera";
            chkRememberServer.Location = new Point(20, 245);
            chkRememberServer.Size = new Size(200, 20);
            chkRememberServer.Checked = true;
            this.Controls.Add(chkRememberServer);


            btnLogin = new Button();
            btnLogin.Text = "Zaloguj";
            btnLogin.Location = new Point(120, 280);
            btnLogin.Size = new Size(80, 35);
            btnLogin.BackColor = Color.DodgerBlue;
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            btnRegister = new Button();
            btnRegister.Text = "Rejestruj";
            btnRegister.Location = new Point(210, 280);
            btnRegister.Size = new Size(80, 35);
            btnRegister.BackColor = Color.ForestGreen;
            btnRegister.ForeColor = Color.White;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnRegister.Click += BtnRegister_Click;
            this.Controls.Add(btnRegister);

            btnCancel = new Button();
            btnCancel.Text = "Anuluj";
            btnCancel.Location = new Point(300, 280);
            btnCancel.Size = new Size(80, 35);
            btnCancel.BackColor = Color.Gray;
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);


            this.AcceptButton = btnLogin;
            this.CancelButton = btnCancel;


            txtNick.Focus();
        }

        private void CbServerPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            var panel = (Panel)cbServerPresets.Tag;
            panel.Visible = cbServerPresets.SelectedIndex == 2;
            
            if (cbServerPresets.SelectedIndex == 1)
            {
                txtServerIP.Text = GetLocalIPAddress();
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "192.168.1.100";
            }
            catch
            {
                return "192.168.1.100";
            }
        }

        private void ChkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                IsRegister = false;
                SetResultValues();
                this.DialogResult = DialogResult.OK;
                if (chkRememberServer.Checked)
                    SaveServerSettings();
                this.Close();
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                IsRegister = true;
                SetResultValues();
                this.DialogResult = DialogResult.OK;
                if (chkRememberServer.Checked)
                    SaveServerSettings();
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtNick.Text))
            {
                MessageBox.Show("Wprowadź nick!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNick.Focus();
                return false;
            }

            if (txtNick.Text.Length < 3)
            {
                MessageBox.Show("Nick musi mieć co najmniej 3 znaki!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNick.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Wprowadź hasło!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Text.Length < 6)
            {
                MessageBox.Show("Hasło musi mieć co najmniej 6 znaków!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (cbServerPresets.SelectedIndex == 2 && string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                MessageBox.Show("Wprowadź adres IP serwera!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtServerIP.Focus();
                return false;
            }

            return true;
        }

        private void SetResultValues()
        {
            Nick = txtNick.Text.Trim();
            Password = txtPassword.Text;
            
            switch (cbServerPresets.SelectedIndex)
            {
                case 0:
                    ServerIP = "127.0.0.1";
                    ServerPort = 8080;
                    break;
                case 1:
                    ServerIP = "9.223.113.121";
                    ServerPort = 8080;
                    break;
                case 2:
                    ServerIP = txtServerIP.Text.Trim();
                    ServerPort = (int)nudPort.Value;
                    break;
            }
        }

        private void LoadServerSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;
                if (!string.IsNullOrEmpty(settings.LastServerIP))
                {
                    txtServerIP.Text = settings.LastServerIP;
                }
                if (settings.LastServerPort > 0)
                {
                    nudPort.Value = settings.LastServerPort;
                }
                cbServerPresets.SelectedIndex = settings.LastServerType;
            }
            catch
            {

            }
        }

        private void SaveServerSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;
                settings.LastServerIP = ServerIP;
                settings.LastServerPort = ServerPort;
                settings.LastServerType = cbServerPresets.SelectedIndex;
                settings.Save();
            }
            catch
            {

            }
        }
    }
}