namespace MocBakery.GUI.Forms.Catalog;
internal sealed class EditDialog : Form
{
    private readonly ErrorProvider errors;
    internal EditDialog(string title, (string Label, string Key, Control Input)[] fields, Func<Task> save)
    {
        Ui.Style(this,title); int height=Math.Min(650,Math.Max(360,200+fields.Length*75));
        MinimumSize = new Size(600,Math.Min(height,520)); ClientSize = new Size(660,height);
        MaximizeBox = false; MinimizeBox = false;
        errors = new() { ContainerControl = this };
        var layout = new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(20) };
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,65));
        Controls.Add(layout); layout.Controls.Add(Ui.Title(title),0,0);
        var body=Ui.Flow(true); layout.Controls.Add(body,0,1);
        foreach(var f in fields){f.Input.Name=f.Key;Ui.Field(body,f.Label,f.Input);}
        var buttons=Ui.Flow();layout.Controls.Add(buttons,0,2);
        var mapping=fields.ToDictionary(f=>f.Key,f=>f.Input);
        var submit=Ui.Button("Lưu",async(_,_)=>await Ui.Run(this,errors,mapping,async()=>{
            await save();DialogResult=DialogResult.OK;Close();
        }),true);
        var cancel=Ui.Button("Hủy",(_,_)=>{DialogResult=DialogResult.Cancel;Close();});
        buttons.Controls.Add(submit);buttons.Controls.Add(cancel);AcceptButton=submit;CancelButton=cancel;
    }
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
internal sealed record ChoiceItem(int Id,string Label)
{
    public override string ToString()=>Label;
    internal static ComboBox Combo(IEnumerable<ChoiceItem> values)
    {
        var box=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=310,Margin=new Padding(0,0,25,12)};
        foreach(var value in values) box.Items.Add(value);
        if(box.Items.Count>0)box.SelectedIndex=0;
        return box;
    }
    internal static int? Selected(ComboBox box)=>(box.SelectedItem as ChoiceItem)?.Id;
}
