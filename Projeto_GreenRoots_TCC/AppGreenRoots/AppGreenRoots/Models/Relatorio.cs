namespace AppGreenRoots.Models;

public class Relatorio
{
    public int Id_Relatorio { get; set; }
    public string Nome { get; set; }
    public DateTime? Data_Criacao { get; set; }
    public string Informacao { get; set; }
    
    public int? Fk_Id_Usuario { get; set; }
}