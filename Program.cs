using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// מיקום הקובץ - תיקיית הפרויקט הנוכחית (Directory.GetCurrentDirectory())
string folderPath = Directory.GetCurrentDirectory();
string filePath = Path.Combine(folderPath, "tasks.json");

// הדפסה ל־Console לבדיקה
Console.WriteLine($"מחפש את tasks.json בנתיב: {filePath}");
Console.WriteLine($"הקובץ קיים? {File.Exists(filePath)}");

// הפניה מ-"/" ל-"tasks"
app.MapGet("/", () => Results.Redirect("/tasks"));

// פונקציית טעינה
List<TaskItem> LoadTasks()
{
    if (!File.Exists(filePath)) return new List<TaskItem>();
    var json = File.ReadAllText(filePath);
    return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
}

// פונקציית שמירה
void SaveTasks(List<TaskItem> tasks)
{
    var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
}

// נקודת קצה: קבלת כל המשימות
app.MapGet("/tasks", () =>
{
    var tasks = LoadTasks();
    return Results.Ok(tasks);
});

// נקודת קצה: הוספת משימה חדשה
app.MapPost("/tasks", async (HttpRequest request) =>
{
    var newTask = await request.ReadFromJsonAsync<TaskItem>();
    if (newTask == null || string.IsNullOrWhiteSpace(newTask.Title))
        return Results.BadRequest("Invalid task data.");

    var tasks = LoadTasks();
    newTask.Id = tasks.Any() ? tasks.Max(t => t.Id) + 1 : 1;
    tasks.Add(newTask);
    SaveTasks(tasks);
    return Results.Ok(newTask);
});

// נקודת קצה: עדכון משימה לפי מזהה
app.MapPut("/tasks/{id:int}", async (int id, HttpRequest request) =>
{
    var updatedTask = await request.ReadFromJsonAsync<TaskItem>();
    if (updatedTask == null) return Results.BadRequest("Invalid task data.");

    var tasks = LoadTasks();
    var existingTask = tasks.FirstOrDefault(t => t.Id == id);
    if (existingTask == null) return Results.NotFound();

    existingTask.Title = updatedTask.Title;
    existingTask.Description = updatedTask.Description;
    existingTask.Priority = updatedTask.Priority;
    existingTask.DueDate = updatedTask.DueDate;
    existingTask.Status = updatedTask.Status;
    SaveTasks(tasks);
    return Results.Ok(existingTask);
});

// נקודת קצה: מחיקת משימה לפי מזהה
app.MapDelete("/tasks/{id:int}", (int id) =>
{
    var tasks = LoadTasks();
    var taskToRemove = tasks.FirstOrDefault(t => t.Id == id);
    if (taskToRemove == null) return Results.NotFound();

    tasks.Remove(taskToRemove);
    SaveTasks(tasks);
    return Results.Ok();
});

app.Run();

// מחלקת המשימה
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Open";
}