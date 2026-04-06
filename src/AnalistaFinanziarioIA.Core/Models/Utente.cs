namespace AnalistaFinanziarioIA.Core.Models
{
    public class Utente
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string ValutaBase { get; set; } = "EUR";

        // Relazione: un utente ha molti asset
        public ICollection<AssetPortafoglio> Assets { get; set; } = new List<AssetPortafoglio>();
    }
}
