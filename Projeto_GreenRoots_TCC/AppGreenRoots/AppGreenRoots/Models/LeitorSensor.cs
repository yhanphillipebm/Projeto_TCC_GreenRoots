namespace AppGreenRoots.Models;

public class LeitorSensor
{
    public int Id_Leitura { get; set; }
    public double Valor { get; set; }
    public  DateTime? Data_Hora { get; set; }
    public string Tipo_Medicao { get; set; }
    
    public int? Fk_Id_Sensor { get; set; }
}