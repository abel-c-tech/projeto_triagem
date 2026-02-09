using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configura o banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// 🔹 Habilita controllers
builder.Services.AddControllers();

var app = builder.Build();

// 🔹 Configura rotas
app.MapControllers();

app.Run();
