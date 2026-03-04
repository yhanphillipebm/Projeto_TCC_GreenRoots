namespace AppGreenRoots.Models;

public class Fornecedores
{
    public int Id_Fornecedor { get; set; }
    public string Nome { get; set; }
    public string Cnpj { get; set; }
    public string pais { get; set; }
    public double Energia_Utilizada { get; set; }
    public int Certificacao_ESG { get; set; }
}