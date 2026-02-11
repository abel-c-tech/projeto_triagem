using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Criar candidato
app.MapPost("/candidatos/texto", async (Person input, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.CurriculoTexto))
        return Results.BadRequest("Texto do currículo é obrigatório.");

    input.Nome = Sanitize(input.Nome);
    input.Email = Sanitize(input.Email);
    input.Tecnologia = Sanitize(input.Tecnologia);
    input.CurriculoTexto = Sanitize(input.CurriculoTexto);

    db.People.Add(input);
    await db.SaveChangesAsync();

    return Results.Ok(new { mensagem = "Currículo salvo via texto!" });
})
.DisableAntiforgery(); // 🔹 desabilita antiforgery

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
        CurriculoTexto = Sanitize(texto)
    };

    db.People.Add(candidato);
    await db.SaveChangesAsync();

    return Results.Ok(new { mensagem = "Currículo salvo com sucesso!", id = candidato.Id });
})
.DisableAntiforgery(); // 🔹 desabilita antiforgery

// 🔹 Comunicação com o agente Python
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
    var json = System.Text.Json.JsonSerializer.Serialize(payload);

    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var response = await client.PostAsync("http://localhost:8000/analisar", content);

    if (!response.IsSuccessStatusCode)
        return Results.Problem("Erro ao comunicar com o agente.");

    var resultado = await response.Content.ReadAsStringAsync();

    return Results.Ok(new
    {
        candidatoId = candidato.Id,
        analise = System.Text.Json.JsonDocument.Parse(resultado)
    });
})
.DisableAntiforgery(); // 🔹 desabilita antiforgery

// 🔹 Listar todos
app.MapGet("/candidatos", async (AppDbContext db) =>
{
    return await db.People.ToListAsync();
});

// 🔹 Filtrar por tecnologia
app.MapGet("/candidatos/tecnologia/{tec}", async (string tec, AppDbContext db) =>
{
    var candidatos = await db.People
        .Where(p => p.Tecnologia.ToLower().Contains(tec.ToLower()))
        .ToListAsync();

    return candidatos;
});

// 🔹 Atualizar candidato
app.MapPut("/candidatos/{id}", async (int id, Person input, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);

    if (person == null)
        return Results.NotFound();

    person.Nome = Sanitize(input.Nome);
    person.Email = Sanitize(input.Email);
    person.Tecnologia = Sanitize(input.Tecnologia);
    person.CurriculoTexto = Sanitize(input.CurriculoTexto);

    await db.SaveChangesAsync();

    return Results.Ok(person);
})
.DisableAntiforgery(); // 🔹 desabilita antiforgery

// 🔹 Deletar candidato
app.MapDelete("/candidatos/{id}", async (int id, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);

    if (person == null)
        return Results.NotFound();

    db.People.Remove(person);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.DisableAntiforgery(); // 🔹 desabilita antiforgery

string Sanitize(string input)
{
    if (string.IsNullOrEmpty(input))
        return input;

    return Regex.Replace(input, @"<.*?>|[^\w\s@.\-]", string.Empty);
}

app.Run();
