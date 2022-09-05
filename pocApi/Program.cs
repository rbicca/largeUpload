var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(o => o.AddDefaultPolicy(x =>
    x.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
));

var app = builder.Build();
app.UseCors();

app.UseHttpsRedirection();

IConfiguration configuration = app.Configuration;
int chunkSize = 1048576 * Convert.ToInt32(configuration["ChunkSize"]);
string tempFolder = configuration["TargetFolder"];


app.MapPost("/UploadChunks", async (string id, string fileName, HttpRequest request) =>
{
    ResponseContext _responseData = new ResponseContext();

    try
    {
        var chunkNumber = id;
        string newpath = Path.Combine(tempFolder + "/Temp", fileName + chunkNumber);
        using (FileStream fs = System.IO.File.Create(newpath))
        {
            byte[] bytes = new byte[chunkSize];
            int bytesRead = 0;
            while ((bytesRead = await request.Body.ReadAsync(bytes, 0, bytes.Length)) > 0)
            {
                fs.Write(bytes, 0, bytesRead);
            }
        }
    }
    catch (Exception ex)
    {
        _responseData.ErrorMessage = ex.Message;
        _responseData.IsSuccess = false;
    }
    return Results.Ok(_responseData);
});

app.MapPost("/UploadComplete", (string fileName) =>
{
    ResponseContext _responseData = new ResponseContext();

    try
    {
        string tempPath = tempFolder + "/Temp";
        string newPath = Path.Combine(tempPath, fileName);
        string[] filePaths = Directory.GetFiles(tempPath).Where(p => p.Contains(fileName)).OrderBy(p => Int32.Parse(p.Replace(fileName, "$").Split('$')[1])).ToArray();
        foreach (string filePath in filePaths)
            MergeChunks(newPath, filePath);
        System.IO.File.Move(Path.Combine(tempPath, fileName), Path.Combine(tempFolder, fileName));
    }
    catch (Exception ex)
    {
        _responseData.ErrorMessage = ex.Message;
        _responseData.IsSuccess = false;
    }
    return Results.Ok(_responseData);
});

static void MergeChunks(string chunk1, string chunk2)
{
    FileStream? fs1 = null;
    FileStream? fs2 = null;
    try
    {
        fs1 = System.IO.File.Open(chunk1, FileMode.Append);
        fs2 = System.IO.File.Open(chunk2, FileMode.Open);
        byte[] fs2Content = new byte[fs2.Length];
        fs2.Read(fs2Content, 0, (int)fs2.Length);
        fs1.Write(fs2Content, 0, (int)fs2.Length);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message + " : " + ex.StackTrace);
    }
    finally
    {
        if (fs1 != null) fs1.Close();
        if (fs2 != null) fs2.Close();
        System.IO.File.Delete(chunk2);
    }
}


app.Run();


public class ResponseContext
{
    public dynamic? Data { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
