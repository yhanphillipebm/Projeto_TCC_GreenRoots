namespace AppGreenRoots.Models;

public class MateriaPrima
{
    public int Id_Materia { get; set; }
    public string Nome { get; set; }
    public  string Tipo { get; set; }
    public double Peso { get; set; }
    public double Fator_Carbono { get; set; }
    
    public int? Fk_Id_Fornecedor { get; set; }
}