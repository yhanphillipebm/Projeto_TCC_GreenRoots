namespace AppGreenRoots.Models;

public class RegistroCarbono
{
    public int Id_Registro { get; set; }
    public double Emissao_CO2 { get; set; }
    public double Energia_Consumida { get; set; }
    public DateTime? Data_Hora { get; set; }
    
    public int? Fk_Id_Componente { get; set; }
}