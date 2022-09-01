using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OrderDb>(opt => opt.UseInMemoryDatabase("Orders"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(i => i.AddPolicy("AllowAnyOrigin",
    builder =>
    {
        builder.AllowAnyMethod()
           .AllowAnyHeader()
           .SetIsOriginAllowed(origin => true)
           .AllowCredentials();
    }
));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pre-Order Api v1"));
app.UseCors("AllowAnyOrigin");

app.MapGet("/", () => "Welcome to Pre-Order API!");

app.MapGet("/orders", async (OrderDb db) =>
    await db.Orders.ToListAsync());

app.MapGet("/orders/complete", async (OrderDb db) =>
    await db.Orders.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/orders/{id}", async (int id, OrderDb db) =>
    await db.Orders.FindAsync(id)
        is Order order
            ? Results.Ok(order)
            : Results.NotFound());

app.MapPost("/orders", async (Order order, OrderDb db) =>
{
    order.Id = new(Guid.NewGuid().ToString());
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", order);
});

app.MapPut("/orders/{id}", async (int id, Order newOrder, OrderDb db) =>
{
    var order = await db.Orders.FindAsync(id);

    if (order is null) return Results.NotFound();

    order.FullName = newOrder.FullName;
    order.IsComplete = newOrder.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/orders/{id}", async (int id, OrderDb db) =>
{
    if (await db.Orders.FindAsync(id) is Order order)
    {
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        return Results.Ok(order);
    }

    return Results.NotFound();
});

app.Run();

class Order
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("fullName")]
    public string? FullName { get; set; }
    [JsonProperty("email")]
    public string? Email { get; set; }
    [JsonProperty("isComplete")]
    public bool IsComplete { get; set; } = false;
}

class OrderDb : DbContext
{
    public OrderDb(DbContextOptions<OrderDb> options)
    : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
}
