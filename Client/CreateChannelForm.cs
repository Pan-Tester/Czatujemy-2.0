using System;
using System.Drawing;
using System.Windows.Forms;

namespace CzatujiemyClient
{
    public partial class CreateChannelForm : Form
    {
        public string ChannelName { get; private set; }
        public string ChannelIcon { get; private set; }
        public string ChannelPassword { get; private set; }

        private TextBox txtName;
        private ComboBox cbIcon;
        private TextBox txtPassword;
        private CheckBox chkHasPassword;
        private Button btnCreate;
        private Button btnCancel;

        public CreateChannelForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 250);
            this.Text = "Utwórz nowy kanał";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;


            var lblName = new Label();
            lblName.Text = "Nazwa kanału:";
            lblName.Location = new Point(20, 20);
            lblName.Size = new Size(100, 20);
            this.Controls.Add(lblName);

            txtName = new TextBox();
            txtName.Location = new Point(130, 18);
            txtName.Size = new Size(230, 25);
            txtName.MaxLength = 30;
            this.Controls.Add(txtName);


            var lblIcon = new Label();
            lblIcon.Text = "Ikona:";
            lblIcon.Location = new Point(20, 55);
            lblIcon.Size = new Size(100, 20);
            this.Controls.Add(lblIcon);

            cbIcon = new ComboBox();
            cbIcon.Location = new Point(130, 53);
            cbIcon.Size = new Size(100, 25);
            cbIcon.DropDownStyle = ComboBoxStyle.DropDownList;
            cbIcon.Items.AddRange(new string[] { "💬", "🎮", "📚", "🎵", "⚽", "🍕", "💻", "🎨", "🌟", "🔥", "💡", "🚀" });
            cbIcon.SelectedIndex = 0;
            this.Controls.Add(cbIcon);


            chkHasPassword = new CheckBox();
            chkHasPassword.Text = "Kanał chroniony hasłem";
            chkHasPassword.Location = new Point(20, 90);
            chkHasPassword.Size = new Size(200, 25);
            chkHasPassword.CheckedChanged += ChkHasPassword_CheckedChanged;
            this.Controls.Add(chkHasPassword);

            var lblPassword = new Label();
            lblPassword.Text = "Hasło:";
            lblPassword.Location = new Point(20, 125);
            lblPassword.Size = new Size(100, 20);
            this.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(130, 123);
            txtPassword.Size = new Size(230, 25);
            txtPassword.MaxLength = 50;
            txtPassword.Enabled = false;
            txtPassword.UseSystemPasswordChar = true;
            this.Controls.Add(txtPassword);


            btnCreate = new Button();
            btnCreate.Text = "Utwórz";
            btnCreate.Location = new Point(200, 170);
            btnCreate.Size = new Size(80, 35);
            btnCreate.DialogResult = DialogResult.OK;
            btnCreate.Click += BtnCreate_Click;
            this.Controls.Add(btnCreate);

            btnCancel = new Button();
            btnCancel.Text = "Anuluj";
            btnCancel.Location = new Point(290, 170);
            btnCancel.Size = new Size(80, 35);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
        }

        private void ChkHasPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = chkHasPassword.Checked;
            if (!chkHasPassword.Checked)
            {
                txtPassword.Clear();
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Wprowadź nazwę kanału!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (chkHasPassword.Checked && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Wprowadź hasło dla kanału!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            ChannelName = txtName.Text;
            ChannelIcon = cbIcon.SelectedItem?.ToString() ?? "💬";
            ChannelPassword = chkHasPassword.Checked ? txtPassword.Text : null;
        }
    }
}