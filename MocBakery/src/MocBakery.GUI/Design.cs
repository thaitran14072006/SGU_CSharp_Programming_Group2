using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI;
internal interface IUnsavedPage { bool CanLeave(); }
internal static class Design
{
    internal static readonly Color Background=Color.FromArgb(248,244,238),Ink=Color.FromArgb(66,46,36),Brown=Color.FromArgb(96,66,50),
        Orange=Color.FromArgb(188,116,75),Muted=Color.FromArgb(149,137,126),Line=Color.FromArgb(235,225,214),Header=Color.FromArgb(238,230,220);
    internal static Button Button(string text,EventHandler action,bool primary=false,bool secondary=false)
    {
        var b=new RoundedButton{Text=text,AutoSize=false,Size=new Size(120,40),FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,
            BackColor=primary?Orange:secondary?Muted:Brown,ForeColor=Color.White,Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(10,0,0,0)};
        b.FlatAppearance.BorderSize=0;b.Click+=action;return b;
    }
    internal static DataGridView Grid(params(string Key,string Label)[] columns)
    {
        var grid=Ui.Grid(columns);grid.Font=new Font("Segoe UI",10);grid.ForeColor=Ink;grid.GridColor=Color.FromArgb(244,239,233);
        grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor=Header;grid.ColumnHeadersDefaultCellStyle.ForeColor=Brown;
        grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=Header;
        grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(245,232,218);grid.DefaultCellStyle.SelectionForeColor=Ink;
        grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(252,250,247);grid.RowTemplate.Height=45;
        grid.ColumnHeadersHeight=40;grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.DefaultCellStyle.Padding=new Padding(10,0,5,0);grid.ColumnHeadersDefaultCellStyle.Padding=new Padding(10,0,0,0);
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="Status")e.CellStyle!.ForeColor=e.Value?.ToString() is "Đang bán" or "Đang dùng" or "Đang hợp tác"?Color.SeaGreen:Muted;};
        grid.BackgroundColor=Background;grid.DefaultCellStyle.BackColor=Color.White;return grid;
    }
    internal static TextBox Search(string hint)=>new(){Width=340,Height=38,AutoSize=false,PlaceholderText=hint,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",10)};
    internal static Label Label(string text)=>new(){Text=text,AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",9),Margin=new Padding(0,0,0,8)};
    internal static Control Field(string label,Control input)
    {
        var p=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Margin=new Padding(0,0,28,15),Height=65};
        p.RowStyles.Add(new(SizeType.Absolute,22));p.RowStyles.Add(new(SizeType.Percent,100));var caption=Label(label);caption.Margin=Padding.Empty;p.Controls.Add(caption,0,0);
        input.Dock=DockStyle.Fill;input.Margin=Padding.Empty;input.Font=new Font("Segoe UI",10);p.Controls.Add(input,0,1);return p;
    }
    internal static NumericUpDown Number(decimal max,int decimals=0)=>new(){Minimum=0,Maximum=max,DecimalPlaces=decimals,ThousandsSeparator=true,Width=310};
    internal static DateTimePicker Date(DateTime value)=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Value=value,Width=310};
    internal static void Open(Control page,Control next){if(page.FindForm() is MainForm main)main.ShowPage(next);}
}
internal sealed class RoundedButton:Button
{
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);if(Width<16||Height<16)return;
        using var path=new System.Drawing.Drawing2D.GraphicsPath();const int diameter=14;
        path.AddArc(0,0,diameter,diameter,180,90);path.AddArc(Width-diameter,0,diameter,diameter,270,90);
        path.AddArc(Width-diameter,Height-diameter,diameter,diameter,0,90);path.AddArc(0,Height-diameter,diameter,diameter,90,90);path.CloseFigure();
        var old=Region;Region=new Region(path);old?.Dispose();
    }
}
internal abstract class BakeryPage:UserControl
{
    protected readonly AppServices Services;
    protected readonly UserSession Session;
    protected readonly TableLayoutPanel LayoutRoot=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(30,24,30,20)};
    protected readonly Panel Body=new(){Dock=DockStyle.Fill,BackColor=Color.White,Margin=Padding.Empty};
    protected readonly FlowLayoutPanel Tools=new(){Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(0,12,0,0),Margin=Padding.Empty};
    protected readonly FlowLayoutPanel Actions=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(0,14,0,0)};
    protected readonly ErrorProvider Errors;
    private readonly Label recordCount=new(){AutoSize=false,Dock=DockStyle.Left,Width=240,ForeColor=Design.Muted,TextAlign=ContentAlignment.MiddleLeft};
    protected BakeryPage(AppServices services,UserSession session,string title,string subtitle)
    {
        Services=services;Session=session;Dock=DockStyle.Fill;BackColor=Design.Background;ForeColor=Design.Ink;Font=new Font("Segoe UI",10);
        Errors=new(){ContainerControl=this};Controls.Add(LayoutRoot);
        LayoutRoot.RowStyles.Add(new(SizeType.Absolute,110));LayoutRoot.RowStyles.Add(new(SizeType.Absolute,95));
        LayoutRoot.RowStyles.Add(new(SizeType.Percent,100));LayoutRoot.RowStyles.Add(new(SizeType.Absolute,65));
        var header=new Panel{Dock=DockStyle.Fill};
        header.Controls.Add(new Label{Text="Cửa hàng Mộc / "+title,ForeColor=Design.Muted,AutoSize=true,Location=new Point(0,4),Font=new Font("Segoe UI",9)});
        var today=new Label{Text=DateTime.Today.ToString("dd/MM/yyyy"),ForeColor=Design.Muted,AutoSize=false,TextAlign=ContentAlignment.TopRight,Size=new Size(130,22),Anchor=AnchorStyles.Top|AnchorStyles.Right};
        header.Controls.Add(today);header.SizeChanged+=(_,_)=>today.Location=new Point(Math.Max(0,header.Width-130),4);
        header.Controls.Add(new Label{Text=title,Font=new Font("Segoe UI",24,FontStyle.Bold),AutoSize=true,Location=new Point(0,30)});
        header.Controls.Add(new Label{Text=subtitle,ForeColor=Design.Muted,AutoSize=true,Location=new Point(0,80),Font=new Font("Segoe UI",9)});
        var footer=new Panel{Dock=DockStyle.Fill};Actions.Dock=DockStyle.Right;Actions.Width=560;footer.Controls.Add(Actions);footer.Controls.Add(recordCount);
        LayoutRoot.Controls.Add(header,0,0);LayoutRoot.Controls.Add(Tools,0,1);LayoutRoot.Controls.Add(Body,0,2);LayoutRoot.Controls.Add(footer,0,3);
    }
    protected void Tool(string label,Control input,int width)
    {
        var panel=new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,Width=width,Height=76,Margin=new Padding(0,0,20,0)};
        panel.Controls.Add(Design.Label(label));input.Width=width;input.Height=38;input.Margin=Padding.Empty;panel.Controls.Add(input);Tools.Controls.Add(panel);
    }
    protected void Primary(string text,EventHandler action)
    {
        var button=Design.Button(text,action,true);button.Width=220;button.Margin=new Padding(25,24,0,0);Tools.Controls.Add(button);
    }
    protected void Count(int count)
    {
        recordCount.Text=count==0?"Chưa có dữ liệu":$"Hiển thị {count} bản ghi";
    }
    protected Task Run(Func<Task> action,Dictionary<string,Control>? fields=null)=>Ui.Run(this,Errors,fields??[],action);
    protected override void Dispose(bool disposing){if(disposing)Errors.Dispose();base.Dispose(disposing);}
}
