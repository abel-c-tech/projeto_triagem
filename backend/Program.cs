using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// HttpClient para falar com o agente Python
builder.Services.AddHttpClient();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


// =========================
// 📌 CADASTRO DE CURRÍCULO
// =========================

// 🔹 Cadastro colando o texto
app.MapPost("/candidatos/texto", async (Person input, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.CurriculoTexto))
        return Results.BadRequest("Texto do currículo é obrigatório.");

    input.Nome = Sanitizar(input.Nome);
    input.Email = Sanitizar(input.Email);
    input.Tecnologia = Sanitizar(input.Tecnologia);
    input.CurriculoTexto = Sanitizar(input.CurriculoTexto);

    db.People.Add(input);
    await db.SaveChangesAsync();

    return Results.Created($"/candidatos/{input.Id}", input);
});


// 🔹 Upload de arquivo TXT
app.MapPost("/candidatos/upload", async (IFormFile arquivo, AppDbContext db) =>
{
    if (arquivo == null || arquivo.Length == 0)
        return Results.BadRequest("Arquivo inválido.");

    if (!arquivo.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Apenas arquivos .txt são permitidos.");

    string texto;
    using (var reader = new StreamReader(arquivo.OpenReadStream()))
    {
        texto = await reader.ReadToEndAsync();
    }

    if (string.IsNullOrWhiteSpace(texto))
        return Results.BadRequest("O arquivo está vazio.");

    var candidato = new Person
    {
        Nome = "Upload TXT",
        CurriculoTexto = Sanitizar(texto)
    };

    db.People.Add(candidato);
    await db.SaveChangesAsync();

    return Results.Created($"/candidatos/{candidato.Id}", candidato);
})
.DisableAntiforgery();



// =========================
// 📌 CONSULTA E MANUTENÇÃO
// =========================

// 🔹 Listar todos
app.MapGet("/candidatos", async (AppDbContext db) =>
{
    return await db.People.ToListAsync();
});


// 🔹 Filtrar por tecnologia (RF05)
app.MapGet("/candidatos/tecnologia/{tec}", async (string tec, AppDbContext db) =>
{
    var candidatos = await db.People
        .Where(p => p.Tecnologia != null &&
                    p.Tecnologia.ToLower().Contains(tec.ToLower()))
        .ToListAsync();

    return candidatos;
});


// 🔹 Atualizar candidato
app.MapPut("/candidatos/{id}", async (int id, Person input, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);

    if (person == null)
        return Results.NotFound("Candidato não encontrado.");

    person.Nome = Sanitizar(input.Nome);
    person.Email = Sanitizar(input.Email);
    person.Tecnologia = Sanitizar(input.Tecnologia);
    person.CurriculoTexto = Sanitizar(input.CurriculoTexto);

    await db.SaveChangesAsync();

    return Results.Ok(person);
});


// 🔹 Deletar candidato
app.MapDelete("/candidatos/{id}", async (int id, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);

    if (person == null)
        return Results.NotFound("Candidato não encontrado.");

    db.People.Remove(person);
    await db.SaveChangesAsync();

    return Results.NoContent();
});


// =========================
// 📌 INTEGRAÇÃO COM AGENTE
// =========================

app.MapPost("/candidatos/analisar/{id}", async (
    int id,
    AppDbContext db,
    IHttpClientFactory httpClientFactory
) =>
{
    var candidato = await db.People.FindAsync(id);

    if (candidato == null)
        return Results.NotFound("Candidato não encontrado.");

    if (string.IsNullOrWhiteSpace(candidato.CurriculoTexto))
        return Results.BadRequest("Currículo não possui texto para análise.");

    var client = httpClientFactory.CreateClient();

    var payload = new { texto = candidato.CurriculoTexto };
    var json = JsonSerializer.Serialize(payload);

    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync(
        "http://localhost:8000/analisar",
        content
    );

    if (!response.IsSuccessStatusCode)
        return Results.Problem("Erro ao comunicar com o agente Python.");

    var resultado = await response.Content.ReadAsStringAsync();

    return Results.Ok(new
    {
        candidatoId = candidato.Id,
        analise = JsonDocument.Parse(resultado)
    });
});


// =========================
// 📌 FUNÇÃO DE SANITIZAÇÃO
// =========================

string Sanitizar(string texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        return texto;

    return Regex.Replace(texto, @"<.*?>|[^\w\s@.\-]", string.Empty);
}

app.Run();