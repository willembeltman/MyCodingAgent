using SmartComponents.LocalEmbeddings; 

public class DocumentChunk
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public EmbeddingF32 Vector { get; set; }
}

public class SimpleVectorSearch
{
    private readonly LocalEmbedder _embedder;
    private readonly List<DocumentChunk> _database;

    public SimpleVectorSearch(LocalEmbedder embedder, List<DocumentChunk> database)
    {
        _embedder = embedder;
        _database = database;
    }

    public void AddToDatabase(string chunkName, string chunkText)
    {
        // De .Embed() methode geeft een EmbeddingF32 terug
        EmbeddingF32 embedding = _embedder.Embed(chunkText);

        _database.Add(new DocumentChunk
        {
            Name = chunkName,
            Text = chunkText,
            Vector = embedding
        });
    }

    public List<string> Search(string query, int resultTake = 3)
    {
        var queryEmbedding = _embedder.Embed(query);
        return _database
            .Select(entry => new {
                entry.Text,
                // Nu zijn beide objecten van het type EmbeddingF32
                // De Similarity methode herkent dit nu direct
                Score = LocalEmbedder.Similarity(queryEmbedding, entry.Vector)
            })
            .OrderByDescending(x => x.Score)
            .Take(resultTake)
            .Select(x => x.Text)
            .ToList();
    }
}

class Program
{
    static void Main() // Maak hem static voor een console app
    {
        // 1. Initialiseer de embedder. 
        // Je hoeft geen pad op te geven, hij gebruikt standaard het 
        // 'all-MiniLM-L6-v2' model dat in de NuGet package zit.
        using var embedder = new LocalEmbedder();

        var chunks = new List<DocumentChunk>();
        var searchEngine = new SimpleVectorSearch(embedder, chunks);

        // 2. Vul je database (dit doe je normaal gesproken met je Roslyn parser)
        Console.WriteLine("Code inladen...");
        searchEngine.AddToDatabase("AuthService.Login", "public bool Login(string u, string p) { return true; }");
        searchEngine.AddToDatabase("DbConfig.Connect", "public void Connect() { _connection.Open(); }");
        searchEngine.AddToDatabase("UserRepo.Get", "public User Get(int id) { return _db.Users.Find(id); }");

        // 3. Zoeken maar!
        Console.WriteLine("Zoeken naar: 'how to sign in'");
        var resultaten = searchEngine.Search("how to sign in", resultTake: 1);

        foreach (var tekst in resultaten)
        {
            Console.WriteLine("\nGevonden code snippet:");
            Console.WriteLine("-----------------------");
            Console.WriteLine(tekst);
        }
    }
}