namespace backend.Models;

public class Person
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tecnologia { get; set; } = string.Empty;
    public string CurriculoTexto { get; set; } = string.Empty;
}