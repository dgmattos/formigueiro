using AntWorld.Domain;
using AntWorld.Simulation;
using Godot;
namespace AntWorld.Game;

public partial class InspectionCard : PanelContainer
{
    public Ant? SelectedAnt { get; private set; }
    public CellPosition? SelectedBlock { get; private set; }
    private Label title=null!,details=null!;
    public string DetailsText=>details.Text;
    public override void _Ready()
    {
        Visible=false;MouseFilter=MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new("12191ef2"),BorderColor=new("75989c"),
            BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
            CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8,
            ContentMarginLeft=14,ContentMarginRight=14,ContentMarginTop=12,ContentMarginBottom=12});
        var scroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,MouseFilter=MouseFilterEnum.Stop};AddChild(scroll);
        var column=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,MouseFilter=MouseFilterEnum.Pass};column.AddThemeConstantOverride("separation",8);scroll.AddChild(column);
        title=new Label{MouseFilter=MouseFilterEnum.Ignore};title.AddThemeFontSizeOverride("font_size",19);column.AddChild(title);
        var hint=new Label{Text="Esc para fechar",MouseFilter=MouseFilterEnum.Ignore};hint.AddThemeColorOverride("font_color",new("a5b8bc"));column.AddChild(hint);
        column.AddChild(new HSeparator{MouseFilter=MouseFilterEnum.Ignore});
        details=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill,MouseFilter=MouseFilterEnum.Ignore};
        details.AddThemeConstantOverride("line_spacing",5);column.AddChild(details);
    }
    public void Select(Ant ant,SimulationState state){SelectedAnt=ant;SelectedBlock=null;Refresh(state);}
    public void Select(CellPosition block,SimulationState state){SelectedAnt=null;SelectedBlock=state.World.Normalize(block);Refresh(state);}
    public void Clear(){SelectedAnt=null;SelectedBlock=null;Visible=false;}
    public void Refresh(SimulationState state)
    {
        Visible=SelectedAnt is not null || SelectedBlock.HasValue;
        if(SelectedAnt is {} ant){title.Text=$"Indivíduo #{ant.Id}";details.Text=InspectionDetails.Individual(ant);}
        else if(SelectedBlock is {} block){title.Text="Bloco";details.Text=InspectionDetails.Block(state.World,block);}
    }
}
