using AntWorld.Domain;
using AntWorld.Simulation;
using AntWorld.Persistence;
using AntWorld.Infrastructure;
using Godot;
namespace AntWorld.Game;

public partial class Main : Node2D
{
    private PlaybackSettings settings=PlaybackSettings.Default;
    private WindowSettings windowSettings=WindowSettings.Default;
    private RenderingSettings rendering = new();
    private SimulationPreset preset=SimulationPreset.Default;
    private SimulationEngine engine = null!;
    private PlaybackControl playback = new();
    private SimulationClock clock => playback.Clock;
    private WorldView worldView = null!;
    private ObserverHud hud = null!;
    private Camera2D camera = null!;
    private SqliteSnapshotStore? store;
    private SimulationLogSettings diagnosticSettings = new();
    private SimulationDiagnostics? diagnostics;
    private double diagnosticSeconds;
    private int diagnosticTicks;
    private bool diagnosticFailureReported;
    private double sampleSeconds;
    private int sampleTicks;
    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--prepare-assets"))
        {
            SetProcess(false); SetProcessUnhandledInput(false);
            try { AssetPreparation.Run(); GetTree().Quit(); }
            catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
            return;
        }
        try
        {
            var configuration=GameSettingsLoader.Load();
            settings=configuration.Playback;
            windowSettings=configuration.Window;
            preset=configuration.Simulation;
            rendering=configuration.Rendering;
            diagnosticSettings=configuration.Diagnostics;
            windowSettings.Apply(GetWindow());
        }
        catch(Exception ex)
        {
            GD.PushError($"Configuração inválida: {ex.Message}");SetProcess(false);SetProcessUnhandledInput(false);GetTree().Quit(1);return;
        }
        camera = new Camera2D { Zoom = new(.5f, .5f), PositionSmoothingEnabled = false }; AddChild(camera);
        hud = new ObserverHud { Settings=settings }; AddChild(hud);
        hud.SpeedRequested += SetSpeed;
        hud.PauseRequested += TogglePause;
        ReplaceWorld(SimulationFactory.Create(preset.World, preset.Colonies, preset.Survival));
        try
        {
            var dataDirectory = OS.GetEnvironment("ANTWORLD_DATA_DIR");
            var path = string.IsNullOrWhiteSpace(dataDirectory) ? ProjectSettings.GlobalizePath("user://antworld.db") : System.IO.Path.Combine(dataDirectory, "antworld.db");
            store = new SqliteSnapshotStore(path);
        }
        catch (Exception ex) { Report("SQLite indisponível", ex); }
        GD.Print($"AntWorld: settings x1={settings.X1}, x2={settings.X2}, x3={settings.X3}.");
        GD.Print($"AntWorld: window mode={windowSettings.Mode}, windowed size={windowSettings.Width}x{windowSettings.Height}.");
        GD.Print($"AntWorld: cena inicializada, seed {engine.State.World.Configuration.Seed}, {engine.State.World.Width}x{engine.State.World.Height} blocos, {engine.State.Colonies.Count} colônias, {engine.State.Ants.Count} indivíduos.");
        if (OS.GetCmdlineUserArgs().Contains("--smoke-test"))
        {
            try { RunSmokeTest(); }
            catch (Exception ex) { Report("Smoke test falhou", ex); GetTree().Quit(1); }
        }
        var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (capture is not null) _ = CaptureFrame(capture[10..]);
    }
    private void ReplaceWorld(SimulationState state)
    {
        if (worldView is not null) { RemoveChild(worldView); worldView.QueueFree(); }
        if (diagnostics is not null)
        {
            engine.AntDied -= diagnostics.RecordDeath;
            diagnostics.Dispose(); diagnostics = null;
        }
        hud.Inspector.Clear();
        engine = new(state); playback = new(settings); sampleSeconds = 0; sampleTicks = 0;
        diagnosticSeconds = 0; diagnosticTicks = 0; diagnosticFailureReported = false;
        worldView = new WorldView(); worldView.Initialize(state, rendering); AddChild(worldView);
        camera.Position = new(state.Colonies[0].Nest.X * rendering.TileSize, state.World.Surface[state.Colonies[0].Nest.X] * rendering.TileSize);
        hud.Refresh(state, playback, 0);
        if (diagnosticSettings.Enabled)
        {
            try
            {
                var directory = OS.GetEnvironment("ANTWORLD_DATA_DIR");
                if (string.IsNullOrWhiteSpace(directory)) directory = ProjectSettings.GlobalizePath("user://");
                diagnostics = new(System.IO.Path.Combine(directory, "diagnostics"), state, diagnosticSettings);
                engine.AntDied += diagnostics.RecordDeath;
                diagnostics.RecordPlayback(state.Tick, clock.Speed, clock.PendingSeconds, 0);
                diagnostics.Flush();
                GD.Print($"AntWorld: diagnóstico de mortes e população em {diagnostics.FilePath}");
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            { Report("Não foi possível iniciar o diagnóstico", ex); }
        }
    }
    public override void _Process(double delta)
    {
        var processed = clock.Advance(delta, StepSimulation, 100, 450.0/engine.State.Performance.TargetFps);
        worldView.Entities.PresentationTime=engine.State.Tick*SimulationState.TickSeconds+Math.Min(clock.PendingSeconds,SimulationState.TickSeconds);
        sampleTicks += processed;
        diagnosticTicks += processed; diagnosticSeconds += delta;
        if (diagnostics is not null)
        {
            if (diagnosticSeconds >= 5)
            {
                diagnostics.RecordPlayback(engine.State.Tick, clock.Speed, clock.PendingSeconds, diagnosticTicks / diagnosticSeconds);
                diagnosticSeconds = 0; diagnosticTicks = 0;
            }
            diagnostics.Flush();
            if (diagnostics.Failure is {} failure && !diagnosticFailureReported)
            { GD.PushWarning(failure); diagnosticFailureReported = true; }
        }
        sampleSeconds += delta;
        if (sampleSeconds >= .25)
        {
            hud.Refresh(engine.State, playback, sampleTicks / sampleSeconds);
            sampleSeconds = 0; sampleTicks = 0;
        }
        if (GetViewport().GuiGetFocusOwner() is LineEdit) return;
        var movement = new Vector2((Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0));
        var following=worldView.Entities.FollowPosition();
        if(following.HasValue)movement.X=0;
        camera.Position += movement * (float)delta * 650 / camera.Zoom.X;
        if(following is {} followed)camera.Position=new(followed.X,camera.Position.Y);
        camera.Position = new((float)HorizontalWrap.Normalize(camera.Position.X, engine.State.World.Width * rendering.TileSize), camera.Position.Y);
        camera.ForceUpdateScroll();
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Right)) camera.Position -= motion.Relative / camera.Zoom;
        if (input is InputEventMouseButton { Pressed: true } mouse)
        {
            if(mouse.ButtonIndex==MouseButton.Left)
            {
                SelectAt(worldView.Entities.GetGlobalTransformWithCanvas().AffineInverse()*mouse.Position);
                GetViewport().SetInputAsHandled();
            }
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                AdjustCameraZoom(mouse.ButtonIndex == MouseButton.WheelUp);
            }

        }
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Escape: ClearSelection(); break;
            case Key.Plus:
            case Key.Equal:
            case Key.KpAdd: AdjustCameraZoom(true); break;
            case Key.Minus:
            case Key.KpSubtract: AdjustCameraZoom(false); break;
            case Key.Space: TogglePause(); break;
            case Key.Key1: SetSpeed(settings.X1); break;
            case Key.Key2: SetSpeed(settings.X2); break;
            case Key.Key3: SetSpeed(settings.X3); break;
        }
    }
    private void SelectAt(Vector2 point)
    {
        if(worldView.Entities.HitTest(point) is {} ant)hud.Inspector.Select(ant,engine.State);
        else
        {
            var cell=new CellPosition((int)Math.Floor(point.X/rendering.TileSize),(int)Math.Floor(point.Y/rendering.TileSize));
            if(!engine.State.World.Contains(cell)){ClearSelection();return;}
            hud.Inspector.Select(cell,engine.State);
        }
        worldView.Entities.SelectedAnt=hud.Inspector.SelectedAnt;
        worldView.Entities.SelectedBlock=hud.Inspector.SelectedBlock;
    }
    private void ClearSelection()
    {
        hud.Inspector.Clear();worldView.Entities.SelectedAnt=null;worldView.Entities.SelectedBlock=null;
    }
    private void StepSimulation()
    {
        engine.Step(); worldView.Entities.CaptureMovement(); diagnostics?.Observe(engine.State);
    }
    public override void _ExitTree()
    {
        if (diagnostics is null) return;
        engine.AntDied -= diagnostics.RecordDeath;
        diagnostics.Dispose();
    }
    private void AdjustCameraZoom(bool zoomIn)
    {
        const float step = 1.15f;
        var zoom = Mathf.Clamp(camera.Zoom.X * (zoomIn ? step : 1 / step), .25f, 4);
        camera.Zoom = new(zoom, zoom);
    }
    private void SetSpeed(int speed) { playback.Select(speed); diagnostics?.RecordPlayback(engine.State.Tick, clock.Speed, clock.PendingSeconds, 0); hud.Refresh(engine.State,playback,0); }
    private void TogglePause() { playback.TogglePause(); diagnostics?.RecordPlayback(engine.State.Tick, clock.Speed, clock.PendingSeconds, 0); hud.Refresh(engine.State,playback,0); }
    private void Report(string message, Exception exception)
    {
        hud.Notify($"{message}: {exception.Message}"); GD.PushError($"{message}: {exception}");
    }
    private void RunSmokeTest()
    {
        if (store is null) throw new InvalidOperationException("SQLite não foi inicializado.");
        AntSpriteAtlas.ValidateImportedTexture(GD.Load<Texture2D>(AntSpriteAtlas.ResourcePath));
        AntSpriteAtlas.ValidateImportedTexture(GD.Load<Texture2D>(AntSpriteAtlas.StrongResourcePath));
        AntSpriteAtlas.ValidateImportedTexture(GD.Load<Texture2D>(AntSpriteAtlas.SurfaceResourcePath), 3);
        new PreparedSpriteCatalog().Validate();
        var seasonalTilesets = new SeasonTilesets();
        var atlasPaths = new HashSet<string>();
        for (var season = 0; season < 4; season++)
        {
            var atlas = seasonalTilesets.ForSeason(season);
            if (atlas.GetWidth() != 1536 || atlas.GetHeight() != 1024 || !atlasPaths.Add(atlas.ResourcePath))
                throw new InvalidOperationException("Atlas sazonal ausente, duplicado ou com dimensões incompatíveis.");
        }
        foreach (var region in engine.State.World.Chunks())
        {
            var sites = Enumerable.Range(0, region.Width * region.Height).Count(i => MushroomHabitat.Allows(engine.State.World, region.Position(i)));
            var count = engine.State.World.CountMushrooms(region);
            if (count < Math.Min(sites * 2, preset.World.Mushrooms.MinimumPerChunk) || count > preset.World.Mushrooms.MaximumPerChunk)
                throw new InvalidOperationException("Quantidade inicial de cogumelos fora dos limites por chunk.");
        }
        if (engine.State.World.Configuration!=preset.World || engine.State.Colonies.Count!=preset.Colonies.Count ||
            engine.State.Ants.Count!=preset.Colonies.Sum(c=>c.Population))
            throw new InvalidOperationException("O mundo e a população inicial não correspondem ao settings.json.");
        if (windowSettings.Mode=="windowed" && GetWindow().Size!=new Vector2I(windowSettings.Width,windowSettings.Height))
            throw new InvalidOperationException("O tamanho da janela não corresponde ao settings.json.");
        if (DisplayServer.GetName()!="headless")
        {
            var expectedMode=windowSettings.Mode=="fullscreen" ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
            if(GetWindow().Mode!=expectedMode) throw new InvalidOperationException("O modo da janela não corresponde ao settings.json.");
        }
        hud.GetNode<TextureButton>("Root/TimePanel/Buttons/Speed2").EmitSignal(BaseButton.SignalName.Pressed);
        clock.Advance(.1, engine.Step,2000);
        var expectedTicks=settings.X2*2;
        if (engine.State.Tick != expectedTicks) throw new InvalidOperationException("Velocidade não aplicou settings.json.");
        hud.GetNode<TextureButton>("Root/TimePanel/Buttons/PausePlay").EmitSignal(BaseButton.SignalName.Pressed);
        clock.Advance(10, engine.Step);
        if (engine.State.Tick != expectedTicks) throw new InvalidOperationException("Pausa não congelou o mundo.");
        var codec = new BinarySnapshotCodec();
        var before = codec.Encode(engine.State);
        store.Save("smoke-test", engine.State);
        engine.Run(10);
        ReplaceWorld(store.Load("smoke-test"));
        if (!before.SequenceEqual(codec.Encode(engine.State))) throw new InvalidOperationException("Save/load Godot divergiu.");
        if(hud.ActionButtonCount!=4) throw new InvalidOperationException("HUD deve conter apenas quatro controles de tempo.");
        TogglePause();
        hud.Refresh(engine.State, playback, 0);
        if(!hud.ShowsPlay) throw new InvalidOperationException("Pausa deve exibir apenas play.");
        SetSpeed(settings.X3);
        if(!playback.IsPaused) throw new InvalidOperationException("Selecionar velocidade não deve desfazer pausa.");
        TogglePause();
        if(hud.ShowsPlay || clock.Speed!=settings.X3) throw new InvalidOperationException("Play deve retomar a velocidade selecionada e exibir pausa.");
        TogglePause();
        GD.Print("AntWorld: smoke test PASS — 90 recortes nos três atlas de indivíduos com transparência, quatro atlas sazonais, limites de cogumelos, x1/x2/x3, pausa/play exclusivos, SQLite, restauração e HUD restrito.");
        _ = ValidateVisualScene();
    }
    private async Task ValidateVisualScene()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ValidateInspection();
            worldView.ValidateTerrainLayers();
            var population = engine.State.World.Vegetation.All.ToArray();
            if (preset.World.Vegetation.Enabled && population.Length == 0) throw new InvalidOperationException("Preset sem vegetação.");
            GD.Print($"AntWorld: TileMapLayer PASS — tile={rendering.TileSize}px, chunk={preset.World.ChunkSize}, vegetação=" + string.Join(", ", population.GroupBy(v => v.Kind).Select(g => $"{g.Key}:{g.Count()}")));
            var output = OS.GetEnvironment("ANTWORLD_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(output)) VisualPreviewExporter.Export(engine.State.World, new PreparedSpriteCatalog(), output);
            await ValidateCircularStreaming();
        }
        catch (Exception ex) { Report("Validação visual falhou", ex); GetTree().Quit(1); }
    }
    private async Task ValidateCircularStreaming()
    {
        var originalPosition = camera.Position; var originalZoom = camera.Zoom;
        var before = new BinarySnapshotCodec().Encode(engine.State);
        var checkedCells = 0; var scenarios = 0;
        SetProcess(false); // Camera test must not advance biology or normalize the deliberately distant positions.
        try
        {
            var width = engine.State.World.Width * rendering.TileSize;
            foreach (var zoom in new[] { .25f, 1f })
            foreach (var x in new[] { -4f * width - 8, -8f, 8f, width - 8f, width + 8f, 4f * width + 8 })
            {
                camera.Zoom = Vector2.One * zoom;
                camera.Position = new(x, engine.State.World.Height * rendering.TileSize / 2f);
                camera.ForceUpdateScroll();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var count = worldView.ValidateVisibleTerrainCoverage();
                if (count == 0) throw new InvalidOperationException("Cenário lateral sem tiles sólidos para validar.");
                if (worldView.Entities.RenderedCount > engine.State.Performance.MaxRenderedAgents)
                    throw new InvalidOperationException("Cópias visuais excederam limite de indivíduos renderizados.");
                checkedCells += count; scenarios++;
            }
            if (!before.SequenceEqual(new BinarySnapshotCodec().Encode(engine.State)))
                throw new InvalidOperationException("Mover câmera alterou estado persistente da simulação.");
            GD.Print($"AntWorld: streaming circular PASS — {scenarios} cenários, {checkedCells} células sólidas verificadas, emendas esquerda/direita, múltiplas voltas, zoom 0.25/1 e estado lógico preservado.");
        }
        finally
        {
            camera.Position = originalPosition; camera.Zoom = originalZoom; camera.ForceUpdateScroll(); SetProcess(true);
        }
    }
    private async Task CaptureFrame(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok) GD.PushError($"Captura falhou: {error}");
        else GD.Print($"AntWorld: captura salva em {path}");
    }

}
