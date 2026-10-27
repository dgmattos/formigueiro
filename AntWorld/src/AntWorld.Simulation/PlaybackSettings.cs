using System.Text.Json;
namespace AntWorld.Simulation;
public sealed class PlaybackSettings(int x1,int x2,int x3)
{
    public int X1 { get; }=x1;
    public int X2 { get; }=x2;
    public int X3 { get; }=x3;
    public static PlaybackSettings Default { get; }=new(1,50,100);
    public IReadOnlyList<int> Multipliers { get; }=Array.AsReadOnly(new[]{x1,x2,x3});
    public void Validate()
    {
        if(X1<1 || X2<=X1 || X3<=X2 || X3>1000)
            throw new ArgumentException("settings.json: use multiplicadores inteiros em ordem crescente, entre 1 e 1000 (x1 < x2 < x3).");
    }
    public static PlaybackSettings Parse(string json)
    {
        try
        {
            using var document=JsonDocument.Parse(json);
            var speeds=document.RootElement.GetProperty("speedMultipliers");
            var settings=new PlaybackSettings(speeds.GetProperty("x1").GetInt32(),speeds.GetProperty("x2").GetInt32(),speeds.GetProperty("x3").GetInt32());
            settings.Validate();return settings;
        }
        catch(Exception ex) when(ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        { throw new ArgumentException("settings.json inválido: informe speedMultipliers com x1, x2 e x3 inteiros.",ex); }
    }
}
