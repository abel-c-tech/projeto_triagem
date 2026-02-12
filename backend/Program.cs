using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

using System.Text.RegularExpressions;


var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

// HttpClient para falar com o agente Python
builder.Services.AddHttpClient();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI();

// =====================================================
// 🔹 ENDPOINT PRINCIPAL: /ask
// Recebe texto ou upload convertido para texto
// =====================================================
app.MapPost("/ask", async (
    HttpRequest request,
    AppDbContext db,
    IHttpClientFactory httpClientFactory
) =>

{
    using var reader = new StreamReader(request.Body);
    var texto = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(texto))
        return Results.BadRequest("Texto do currículo é obrigatório.");


    var textoLimpo = Sanitize(texto);


    var client = httpClientFactory.CreateClient();
    var payload = new { texto = textoLimpo };
    var json = System.Text.Json.JsonSerializer.Serialize(payload);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var response = await client.PostAsync("http://localhost:8000/analisar", content);

    if (!response.IsSuccessStatusCode)
        return Results.Problem("Erro ao comunicar com o agente NLP.");

    var resultado = await response.Content.ReadAsStringAsync();
    var analiseObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(resultado);

    // só salva se análise deu certo
    var candidato = new Person
    {
        Nome = "Frontend",
        CurriculoTexto = textoLimpo
    };

    db.People.Add(candidato);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        candidatoId = candidato.Id,
        analise = analiseObj
    });
})
.DisableAntiforgery();

// =====================================================
// 🔹 UPLOAD DE ARQUIVO
// Agora chama o agente igual ao /ask
// =====================================================
app.MapPost("/candidatos/upload", async (IFormFile arquivo, AppDbContext db, IHttpClientFactory httpClientFactory) =>
{
    if (arquivo == null || arquivo.Length == 0)
        return Results.BadRequest("Arquivo inválido.");

    if (!arquivo.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Apenas arquivos .txt são permitidos.");

    string texto;
    using (var reader = new StreamReader(arquivo.OpenReadStream()))
        texto = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(texto))
        return Results.BadRequest("O arquivo está vazio.");

    var textoLimpo = Sanitize(texto);

    // Salva candidato
    var candidato = new Person
    {
        Nome = "Upload TXT",

        CurriculoTexto = textoLimpo

    };
    db.People.Add(candidato);
    await db.SaveChangesAsync();

    // Chama agente Python
    var client = httpClientFactory.CreateClient();
    var payload = new { texto = textoLimpo };
    var json = System.Text.Json.JsonSerializer.Serialize(payload);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var response = await client.PostAsync("http://localhost:8000/analisar", content);
    if (!response.IsSuccessStatusCode)
        return Results.Problem("Erro ao comunicar com o agente NLP.");


    var resultado = await response.Content.ReadAsStringAsync();
    var analiseObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(resultado);

    return Results.Ok(new
    {
        candidatoId = candidato.Id,
        analise = analiseObj
    });
})
.DisableAntiforgery();


// =====================================================
// 🔹 Listar candidatos
// =====================================================
app.MapGet("/candidatos", async (AppDbContext db) =>
{
    return await db.People.ToListAsync();
});

// =====================================================
// 🔹 Deletar candidato
// =====================================================
app.MapDelete("/candidatos/{id}", async (int id, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);
    if (person == null) return Results.NotFound();

    db.People.Remove(person);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.DisableAntiforgery();


// =====================================================
// 🔧 Utils
// =====================================================
string Sanitize(string input)
{
    if (string.IsNullOrEmpty(input))
        return input;

    return Regex.Replace(input, @"<.*?>|[^\w\s@.\-]", string.Empty);
}


app.Run();
record AskRequest(string CurriculoTexto);
