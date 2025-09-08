public class TrafficEvent
{
    public int timeInMinutes; // minutos desde 00:00
    public string ruta;
    public int cantidad;
    public int autoPct;
    public int camionPct;
    public int motoPct;

    public TrafficEvent(
        string hora,
        string ruta,
        int cantidad,
        int autoPct,
        int camionPct,
        int motoPct
    )
    {
        var parts = hora.Split(':');
        int h = int.Parse(parts[0]);
        int m = int.Parse(parts[1]);
        timeInMinutes = h * 60 + m;

        this.ruta      = ruta;
        this.cantidad  = cantidad;
        this.autoPct   = autoPct;
        this.camionPct = camionPct;
        this.motoPct   = motoPct;
    }
}
