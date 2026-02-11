using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

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
});

app.MapPost("/candidatos/upload", async (HttpRequest request, AppDbContext db) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Envie como multipart/form-data.");

    var form = await request.ReadFormAsync();
    var file = form.Files["file"];

    if (file == null || file.Length == 0)
        return Results.BadRequest("Arquivo inválido.");

    if (!file.FileName.EndsWith(".txt"))
        return Results.BadRequest("Apenas arquivos .txt são permitidos.");

    string texto;

    using (var reader = new StreamReader(file.OpenReadStream()))
    {
        texto = await reader.ReadToEndAsync();
    }

    if (string.IsNullOrWhiteSpace(texto))
        return Results.BadRequest("O arquivo está vazio.");

    var person = new Person
    {
        Nome = "Extraído do TXT",
        CurriculoTexto = Sanitize(texto)
    };

    db.People.Add(person);
    await db.SaveChangesAsync();

    return Results.Ok(new { mensagem = "Currículo salvo com sucesso!" });
})
.Accepts<IFormFile>("multipart/form-data");


// 🔹 Listar todos
app.MapGet("/candidatos", async (AppDbContext db) =>
{
    return await db.People.ToListAsync();
});


// 🔹 Filtrar por tecnologia (RF05)
app.MapGet("/candidatos/tecnologia/{tec}", async (string tec, AppDbContext db) =>
{
    var candidatos = await db.People
        .Where(p => p.Tecnologia.ToLower().Contains(tec.ToLower()))
        .ToListAsync();

    return candidatos;
});

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
});

app.MapDelete("/candidatos/{id}", async (int id, AppDbContext db) =>
{
    var person = await db.People.FindAsync(id);

    if (person == null)
        return Results.NotFound();

    db.People.Remove(person);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

string Sanitize(string input)
{
    if (string.IsNullOrEmpty(input))
        return input;

    return Regex.Replace(input, @"<.*?>|[^\w\s@.\-]", string.Empty);
}

app.Run();