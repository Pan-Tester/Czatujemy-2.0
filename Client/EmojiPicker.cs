using System;
using System.Drawing;
using System.Windows.Forms;

namespace CzatujiemyClient
{
    public partial class EmojiPicker : Form
    {
        public string SelectedEmoji { get; private set; }

        private readonly string[] emojis = new string[]
        {
            "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃",
            "😉", "😊", "😇", "🥰", "😍", "🤩", "😘", "😗", "😚", "😙",
            "😋", "😛", "😜", "🤪", "😝", "🤑", "🤗", "🤭", "🤫", "🤔",
            "🤐", "🤨", "😐", "😑", "😶", "😏", "😒", "🙄", "😬", "🤥",
            "😔", "😪", "🤤", "😴", "😷", "🤒", "🤕", "🤢", "🤮", "🤧",
            "🥵", "🥶", "🥴", "😵", "🤯", "🤠", "🥳", "😎", "🤓", "🧐",
            "😤", "😠", "😡", "🤬", "😱", "😨", "😰", "😥", "😢", "😭",
            "😖", "😣", "😞", "😓", "😩", "😫", "🥱", "😤", "😮", "😦",
            "👍", "👎", "👌", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉",
            "👆", "👇", "☝️", "✋", "🤚", "🖐️", "🖖", "👋", "🤙", "💪",
            "❤️", "🧡", "💛", "💚", "💙", "💜", "🤎", "🖤", "🤍", "💯"
        };

        public EmojiPicker()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(350, 300);
            this.Text = "Wybierz emoji";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var panel = new Panel();
            panel.Location = new Point(10, 10);
            panel.Size = new Size(320, 220);
            panel.AutoScroll = true;
            this.Controls.Add(panel);

            int x = 5, y = 5;
            for (int i = 0; i < emojis.Length; i++)
            {
                var btn = new Button();
                btn.Text = emojis[i];
                btn.Size = new Size(30, 30);
                btn.Location = new Point(x, y);
                btn.Font = new Font("Segoe UI Emoji", 12);
                btn.Tag = emojis[i];
                btn.Click += EmojiButton_Click;
                panel.Controls.Add(btn);

                x += 35;
                if (x > 280)
                {
                    x = 5;
                    y += 35;
                }
            }

            var btnCancel = new Button();
            btnCancel.Text = "Anuluj";
            btnCancel.Location = new Point(250, 240);
            btnCancel.Size = new Size(80, 25);
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            this.CancelButton = btnCancel;
        }

        private void EmojiButton_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            SelectedEmoji = btn.Tag.ToString();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}