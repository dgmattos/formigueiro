using AntWorld.Domain;
using AntWorld.Simulation;
using Godot;
namespace AntWorld.Game;
public partial class ObserverHud : CanvasLayer
{
    public PlaybackSettings Settings { get; init; }=PlaybackSettings.Default;
    public event Action<int>? SpeedRequested;
    public event Action? PauseRequested;
    private TextureButton pause=null!;
    private Texture2D playTexture=null!,pauseTexture=null!;
    private Label time=null!,status=null!;
    public InspectionCard Inspector { get; }=new(){Name="InspectionCard",AnchorBottom=1,OffsetLeft=18,OffsetRight=338,OffsetTop=86,OffsetBottom=-54};
    private readonly Dictionary<int,TextureButton> speeds=[];
    public bool ShowsPlay { get; private set; }
    public int ActionButtonCount => speeds.Count+1;
    public override void _Ready()
    {
        var atlas=GD.Load<Texture2D>("res://Assets/time-controls.png");
        Texture2D Region(float x,float width) => new AtlasTexture { Atlas=atlas,Region=new(x,132,width,410),FilterClip=true };
        pauseTexture=Region(22,376); playTexture=Region(398,380);
        var root=new Control { Name="Root", MouseFilter=Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); AddChild(root);
        var panel=new PanelContainer { Name="TimePanel", AnchorLeft=1,AnchorRight=1,OffsetLeft=-316,OffsetRight=-16,OffsetTop=16,OffsetBottom=100 };
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat { BgColor=new("12191ee6"),CornerRadiusTopLeft=8,CornerRadiusBottomLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomRight=8,ContentMarginLeft=6,ContentMarginRight=6,ContentMarginTop=6,ContentMarginBottom=6 });
        root.AddChild(panel);
        var row=new HBoxContainer { Name="Buttons" }; row.AddThemeConstantOverride("separation",4); panel.AddChild(row);
        pause=Button(pauseTexture,"Pausar toda a simulação [Espaço]"); pause.Name="PausePlay"; pause.Pressed+=()=>PauseRequested?.Invoke(); row.AddChild(pause);
        var regions=new[]{Region(902,389),Region(1290,364),Region(1655,373)};
        var multipliers=Settings.Multipliers;
        for(var i=0;i<3;i++)
        {
            var multiplier=multipliers[i]; var button=Button(regions[i],$"x{i+1}: {multiplier}× o tempo e todas as ações [{i+1}]");
            button.Name=$"Speed{i+1}"; button.Pressed+=()=>SpeedRequested?.Invoke(multiplier); speeds.Add(multiplier,button); row.AddChild(button);
        }
        time=new Label { Position=new(18,18),MouseFilter=Control.MouseFilterEnum.Ignore }; root.AddChild(time);
        root.AddChild(Inspector);
        status=new Label { MouseFilter=Control.MouseFilterEnum.Ignore }; status.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft); status.Position=new(18,-32); root.AddChild(status);
        status.Text="Câmera: WASD / arrastar botão direito · Zoom: rolagem ou +/− · Tempo: espaço e 1–3";
    }
    private static TextureButton Button(Texture2D texture,string tooltip) => new()
    {
        TextureNormal=texture,TextureFilter=CanvasItem.TextureFilterEnum.Nearest,IgnoreTextureSize=true,
        StretchMode=TextureButton.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(68,68),
        TooltipText=tooltip,MouseFilter=Control.MouseFilterEnum.Stop,FocusMode=Control.FocusModeEnum.None,MouseDefaultCursorShape=Control.CursorShape.PointingHand
    };
    public void Notify(string message) => status.Text=message;
    public void Refresh(SimulationState state,PlaybackControl playback,double ticksPerSecond)
    {
        var seasons=new[]{"Primavera","Verão","Outono","Inverno"};
        time.Text=$"Dia {state.DayOfMonth}/28 · Mês {state.Month}/4 · {seasons[state.Season]} · Ano {state.Year}\n{state.Ants.Count} indivíduos · Progresso do dia: {state.DayFraction:P1} · {state.Tick * SimulationState.TickSeconds:F0}s simulados";
        var rhythm=playback.IsPaused?"Pausado":$"Tempo: {playback.SelectedMultiplier}×";
        if(!playback.IsPaused && playback.Clock.PendingSeconds>2) rhythm+=" · Aceleração limitada pelo desempenho";
        status.Text=$"{rhythm} · Câmera: WASD / botão direito · Zoom: rolagem ou +/− · Tempo: espaço e 1–3";
        Inspector.OffsetTop=time.Position.Y+time.Size.Y+14;
        Inspector.Refresh(state);
        ShowsPlay=playback.IsPaused;
        pause.TextureNormal=ShowsPlay?playTexture:pauseTexture;
        pause.TooltipText=ShowsPlay?"Retomar a simulação [Espaço]":"Pausar toda a simulação [Espaço]";
        foreach(var (multiplier,button) in speeds) button.Modulate=multiplier==playback.SelectedMultiplier?Colors.White:new(.55f,.55f,.55f);
    }
}
