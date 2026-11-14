using MocBakery.Models;
namespace MocBakery.GUI;
internal static class Ui
{
    internal static readonly Color Cream = Color.FromArgb(250,246,238), Brown = Color.FromArgb(91,61,43),
        Accent = Color.FromArgb(160,107,65), Selection = Color.FromArgb(237,219,192);
    internal static void Style(Form form, string title)
    {
        form.Text = title + " • Mộc Bakery"; form.Font = new Font("Segoe UI",10);
        form.BackColor = Cream; form.ForeColor = Brown; form.StartPosition = FormStartPosition.CenterScreen;
        form.AutoScaleMode = AutoScaleMode.Dpi; form.ClientSize = new Size(1200,660);
        form.MinimumSize = new Size(1000,650);
    }
    internal static Label Title(string text) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI",22,FontStyle.Bold), Margin = new Padding(0,0,0,10) };
    internal static TextBox Input(int length = 255, bool password = false) => new() { Width = 310, MaxLength = length, UseSystemPasswordChar = password, Margin = new Padding(0,0,25,12) };
    internal static Button Button(string text, EventHandler action, bool primary = false)
    {
        var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(110,38), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Brown : Color.White, ForeColor = primary ? Color.White : Brown, Margin = new Padding(0,8,10,8) };
        b.FlatAppearance.BorderColor = Selection; b.Click += action; return b;
    }
    internal static ComboBox Choice(params string[] options)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310, Margin = new Padding(0,0,25,12) };
        c.Items.AddRange(options); c.SelectedIndex = 0; return c;
    }
    internal static void Field(FlowLayoutPanel panel, string label, Control input)
    {
        var box = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0,0,12,3) };
        box.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0,0,0,6) });
        box.Controls.Add(input); panel.Controls.Add(box);
    }
    internal static FlowLayoutPanel Flow(bool vertical = false) => new() { Dock = DockStyle.Fill, AutoScroll = true,
        FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight, WrapContents = !vertical, Padding = new Padding(12) };
    internal static DataGridView Grid(params (string Key,string Title)[] columns)
    {
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false, ColumnHeadersHeight = 38, RowTemplate = { Height = 34 } };
        grid.DefaultCellStyle.SelectionBackColor = Selection; grid.DefaultCellStyle.SelectionForeColor = Brown;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Brown; grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        foreach(var (key,title) in columns) grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = key, Name = key, HeaderText = title });
        return grid;
    }
    internal static async Task Run(Control owner, ErrorProvider errors, Dictionary<string,Control> fields, Func<Task> action)
    {
        errors.Clear(); owner.Enabled = false; owner.UseWaitCursor = true;
        try { await action(); }
        catch(BusinessException ex)
        {
            owner.Enabled = true;
            if(ex.Field is not null && fields.TryGetValue(ex.Field,out var field))
            { errors.SetError(field,ex.Message); field.Focus(); }
            MessageBox.Show(owner,ex.Message,"Kiểm tra thông tin",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            string code = ErrorLog.Write(ex);
            MessageBox.Show(owner,"Không thể hoàn tất thao tác. Kiểm tra cấu hình/kết nối SQL Server và thử lại.\nMã lỗi: " + code,
                "Lỗi hệ thống",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        finally { if(!owner.IsDisposed) { owner.Enabled = true; owner.UseWaitCursor = false; } }
    }
    internal static bool Confirm(Control owner,string message) => MessageBox.Show(owner,message,"Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes;
}
internal static class ErrorLog
{
    internal static string Write(Exception ex)
    {
        string code = Guid.NewGuid().ToString("N")[..10];
        try
        {
            string folder = Path.Combine(AppContext.BaseDirectory,"logs"); Directory.CreateDirectory(folder);
            // Do not log SQL text, parameters, connection strings, passwords or exception messages.
            File.AppendAllText(Path.Combine(folder,"errors.log"),$"{DateTimeOffset.Now:O} {code} {ex.GetType().FullName} HResult={ex.HResult}\n");
        }
        catch(IOException) { }
        catch(UnauthorizedAccessException) { }
        return code;
    }
}
