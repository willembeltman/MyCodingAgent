using SmartComponents.LocalEmbeddings;

namespace MyCodingAgent.Helpers;

public class DocumentChunk
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public EmbeddingF32 Vector { get; set; }
}

public class VectorSearch
{
    private readonly LocalEmbedder Embedder;
    private readonly ICollection<DocumentChunk> Database;

    public VectorSearch(LocalEmbedder embedder, ICollection<DocumentChunk> database)
    {
        Embedder = embedder;
        Database = database;
    }

    public void AddToDatabase(string chunkName, string chunkText)
    {
        // De .Embed() methode geeft een EmbeddingF32 terug
        EmbeddingF32 embedding = Embedder.Embed(chunkText);

        Database.Add(new DocumentChunk
        {
            Name = chunkName,
            Text = chunkText,
            Vector = embedding
        });
    }

    public List<string> Search(string query, int topK = 3)
    {
        // Ook hier is queryEmbedding nu een EmbeddingF32
        EmbeddingF32 queryEmbedding = Embedder.Embed(query);

        return Database
            .Select(entry => new {
                entry.Text,
                // Nu zijn beide objecten van het type EmbeddingF32
                // De Similarity methode herkent dit nu direct
                Score = LocalEmbedder.Similarity(queryEmbedding, entry.Vector)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Text)
            .ToList();
    }
}
