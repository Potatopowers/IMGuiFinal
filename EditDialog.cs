
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProfileApp
{
    public class EditDialog : Form
    {
        private readonly TextBox _text;
        private readonly Button _save;
        private readonly Button _cancel;

        public string EditedText => _text.Text;

        public EditDialog(string title, string initialText)
        {
            Theme.Apply(this);
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 420);

            var label = Theme.BodyText("Enter your content below:");
            label.Margin = new Padding(0, 0, 0, 6);

            _text = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = Theme.TypeScale.Body(),
                Text = initialText
            };

            _save = new Button { Text = "Save" };
            Theme.StylePrimaryButton(_save);
            _save.Click += (_, __) => DialogResult = DialogResult.OK;

            _cancel = new Button { Text = "Cancel" };
            Theme.StyleSecondaryButton(_cancel);
            _cancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 12, 0, 12),
                AutoSize = true
            };
            footer.Controls.Add(_save);
            footer.Controls.Add(_cancel);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 3
            };
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            content.Controls.Add(label, 0, 0);
            content.Controls.Add(_text, 0, 1);
            content.Controls.Add(footer, 0, 2);

            Controls.Add(content);
        }
    }
}
