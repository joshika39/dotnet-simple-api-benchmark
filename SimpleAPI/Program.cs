using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Helper methods
int? GetQueryParam(HttpRequest request, string key, Func<string, int> parser)
{
    if (request.Query.TryGetValue(key, out var value))
    {
        try
        {
            return parser(value);
        }
        catch
        {
            return null;
        }
    }

    return null;
}

int CalculateFactorial(int n)
{
    if (n < 0 || n > 170) throw new ArgumentOutOfRangeException("n", "n must be between 0 and 170");
    return Enumerable.Range(1, n).Aggregate(1, (acc, val) => acc * val);
}

int GetFibonacci(int n)
{
    if (n < 0) throw new ArgumentOutOfRangeException("n", "n must be a non-negative integer");
    if (n == 0) return 0;
    if (n == 1) return 1;

    int a = 0, b = 1;
    for (int i = 2; i <= n; i++)
    {
        int temp = a + b;
        a = b;
        b = temp;
    }

    return b;
}

string StripFilename(string filename)
{
    return new string(filename.Where(char.IsLetterOrDigit).ToArray());
}

// Handlers
app.MapGet("/", () => new Response("Hello, World!", 200));

app.MapGet("/factorial", (HttpRequest request) =>
{
    try
    {
        int? n = GetQueryParam(request, "n", int.Parse);
        if (n == null)
            return Results.BadRequest("Invalid input: n must be a number");

        int factorial = CalculateFactorial(n.Value);
        return Results.Ok(new Response($"Factorial of {n} is {factorial}", 200));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new Response(ex.Message, 400));
    }
});

app.MapGet("/fibonacci", (HttpRequest request) =>
{
    try
    {
        int? n = GetQueryParam(request, "n", int.Parse);
        if (n == null || n < 0)
            return Results.BadRequest("Invalid input: n must be a non-negative number");

        int fibonacci = GetFibonacci(n.Value);
        return Results.Ok(new Response($"Fibonacci of {n} is {fibonacci}", 200));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new Response(ex.Message, 400));
    }
});

app.MapPost("/save", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<SaveContentRequest>(body);

        if (data == null || string.IsNullOrWhiteSpace(data.Content) || string.IsNullOrWhiteSpace(data.Filename))
        {
            return Results.BadRequest("Invalid input");
        }

        var absolutePath = Path.GetFullPath("./files");
        var safeFilename = Regex.Replace(data.Filename, "[^a-zA-Z0-9.]", "");
        var fullPath = Path.Combine(absolutePath, safeFilename);
        if (Path.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        await File.WriteAllTextAsync(fullPath, data.Content);

        return Results.Ok(new Response("File saved successfully", 200));
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/files/{filename}", (string filename) =>
{
    try
    {
        var safeFilename = StripFilename(filename);
        var filePath = Path.Combine("./files", safeFilename);

        if (!File.Exists(filePath))
            return Results.NotFound("File not found");

        var content = File.ReadAllText(filePath);
        return Results.Ok(content);
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.MapGet("/files", () =>
{
    try
    {
        var files = Directory.GetFiles("./files").Select(Path.GetFileName).ToList();
        return Results.Ok(files);
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.Run();

record Response(string Message, int Status);

record SaveContentRequest(string Content, string Filename);