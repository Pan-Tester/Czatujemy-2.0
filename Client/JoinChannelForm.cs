using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CzatujiemyClient
{
    public partial class JoinChannelForm : Form
    {
        public string SelectedChannelId { get; private set; }
        public string Password { get; private set; }

        private ListBox lstAvailableChannels;
        private TextBox txtPassword;
        private Label lblPassword;
        private Button btnJoin;
        private Button btnCancel;
        private Button btnRefresh;
        private List<ChannelInfo> availableChannels = new List<ChannelInfo>();
        private MainForm parentForm;

        public JoinChannelForm(MainForm parent)
        {
            this.parentForm = parent;
            InitializeComponent();
            LoadAvailableChannels();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 400);
            this.Text = "Dołącz do kanału";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;


            var lblChannels = new Label();
            lblChannels.Text = "Dostępne kanały:";
            lblChannels.Location = new Point(20, 20);
            lblChannels.Size = new Size(150, 20);
            this.Controls.Add(lblChannels);

            lstAvailableChannels = new ListBox();
            lstAvailableChannels.Location = new Point(20, 45);
            lstAvailableChannels.Size = new Size(440, 200);
            lstAvailableChannels.DrawMode = DrawMode.OwnerDrawFixed;
            lstAvailableChannels.ItemHeight = 30;
            lstAvailableChannels.DrawItem += LstAvailableChannels_DrawItem;
            lstAvailableChannels.SelectedIndexChanged += LstAvailableChannels_SelectedIndexChanged;
            this.Controls.Add(lstAvailableChannels);


            btnRefresh = new Button();
            btnRefresh.Text = "Odśwież";
            btnRefresh.Location = new Point(380, 15);
            btnRefresh.Size = new Size(80, 25);
            btnRefresh.Click += BtnRefresh_Click;
            this.Controls.Add(btnRefresh);


            lblPassword = new Label();
            lblPassword.Text = "Hasło (jeśli wymagane):";
            lblPassword.Location = new Point(20, 260);
            lblPassword.Size = new Size(150, 20);
            this.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(180, 258);
            txtPassword.Size = new Size(280, 25);
            txtPassword.UseSystemPasswordChar = true;
            this.Controls.Add(txtPassword);


            btnJoin = new Button();
            btnJoin.Text = "Dołącz";
            btnJoin.Location = new Point(300, 310);
            btnJoin.Size = new Size(80, 35);
            btnJoin.DialogResult = DialogResult.OK;
            btnJoin.Click += BtnJoin_Click;
            btnJoin.Enabled = false;
            this.Controls.Add(btnJoin);

            btnCancel = new Button();
            btnCancel.Text = "Anuluj";
            btnCancel.Location = new Point(390, 310);
            btnCancel.Size = new Size(80, 35);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnJoin;
            this.CancelButton = btnCancel;
        }

        private void LstAvailableChannels_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= availableChannels.Count) return;

            var channel = availableChannels[e.Index];
            e.DrawBackground();


            string displayText = $"{channel.Icon} {channel.Name}";
            if (channel.HasPassword) displayText += " 🔒";
            displayText += $" ({channel.MemberCount} członków)";

            var textBrush = new SolidBrush(e.ForeColor);
            var font = e.Font;
            
            e.Graphics.DrawString(displayText, font, textBrush, e.Bounds.Location);
            e.DrawFocusRectangle();
        }

        private void LstAvailableChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnJoin.Enabled = lstAvailableChannels.SelectedIndex >= 0;
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadAvailableChannels();
        }

        private void BtnJoin_Click(object sender, EventArgs e)
        {
            if (lstAvailableChannels.SelectedIndex < 0)
            {
                MessageBox.Show("Wybierz kanał!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedChannel = availableChannels[lstAvailableChannels.SelectedIndex];
            SelectedChannelId = selectedChannel.Id;
            Password = txtPassword.Text;
        }

        private async void LoadAvailableChannels()
        {
            if (parentForm != null)
            {

                await parentForm.RequestAvailableChannels();
            }
        }

        public void UpdateAvailableChannels(List<ChannelInfo> channels)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateAvailableChannels(channels)));
                return;
            }

            availableChannels = channels;
            lstAvailableChannels.Items.Clear();
            foreach (var channel in availableChannels)
            {
                lstAvailableChannels.Items.Add(channel);
            }
        }
    }
}