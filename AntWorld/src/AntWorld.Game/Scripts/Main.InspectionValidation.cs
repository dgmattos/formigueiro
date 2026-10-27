using AntWorld.Domain;
using AntWorld.Infrastructure;
using Godot;
namespace AntWorld.Game;

public partial class Main
{
    private async Task ValidateInspection()
    {
        var originalPosition=camera.Position;var originalZoom=camera.Zoom;
        var before=new BinarySnapshotCodec().Encode(engine.State);
        try
        {
            camera.Zoom=Vector2.One;
            var ant=engine.State.Ants.First(a=>a.IsAlive);
            camera.Position=new(ant.Position.X*rendering.TileSize,ant.Position.Y*rendering.TileSize);
            camera.ForceUpdateScroll();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var point=worldView.Entities.DrawnCenter(ant.Id)??throw new InvalidOperationException("Indivíduo não foi desenhado para testar seleção.");
            var expected=worldView.Entities.HitTest(point)??throw new InvalidOperationException("Sprite desenhado não pôde ser selecionado.");
            await InspectionClick(worldView.Entities.GetGlobalTransformWithCanvas()*point);
            if(hud.Inspector.SelectedAnt!=expected || !hud.Inspector.Visible || !hud.Inspector.DetailsText.Contains("Vida:"))
                throw new InvalidOperationException("Clique no sprite não abriu propriedades.");
            var height=camera.Position.Y+20;
            camera.Position=new(camera.Position.X+100,height);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var followed=worldView.Entities.FollowPosition()!.Value;
            if(Math.Abs(camera.Position.X-followed.X)>.1 || Math.Abs(camera.Position.Y-height)>.1)
                throw new InvalidOperationException("Acompanhamento deve alterar apenas o eixo horizontal.");
            await InspectionClick(hud.Inspector.GetGlobalRect().GetCenter());
            if(hud.Inspector.SelectedAnt!=expected)throw new InvalidOperationException("Card deixou clique atravessar para o mundo.");
            var blockScreen=GetViewportRect().Size*new Vector2(.75f,.5f);
            var blockWorld=worldView.Entities.GetGlobalTransformWithCanvas().AffineInverse()*blockScreen;
            var block=engine.State.World.Normalize(new((int)Math.Floor(blockWorld.X/rendering.TileSize),(int)Math.Floor(blockWorld.Y/rendering.TileSize)));
            await InspectionClick(blockScreen);
            if(hud.Inspector.SelectedAnt is not null || hud.Inspector.SelectedBlock!=block || !hud.Inspector.DetailsText.Contains("Umidade:"))
                throw new InvalidOperationException("Clique no bloco não abriu propriedades.");
            if(worldView.Entities.FollowPosition() is not null)throw new InvalidOperationException("Selecionar bloco não encerrou acompanhamento.");
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,Pressed=true});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventKey{Keycode=Key.Escape,Pressed=false});
            if(hud.Inspector.Visible)throw new InvalidOperationException("Esc não fechou inspeção.");
            if(!before.SequenceEqual(new BinarySnapshotCodec().Encode(engine.State)))
                throw new InvalidOperationException("Inspecionar alterou estado da simulação.");
            GD.Print("AntWorld: inspection PASS — cliques reais em sprite/bloco, card, proteção da interface, acompanhamento horizontal, Esc e estado lógico preservado.");
        }
        finally
        {
            ClearSelection();camera.Position=originalPosition;camera.Zoom=originalZoom;camera.ForceUpdateScroll();
        }
    }
    private async Task InspectionClick(Vector2 screen)
    {
        Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=screen,GlobalPosition=screen});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false,Position=screen,GlobalPosition=screen});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
}
