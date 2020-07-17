namespace PSNotes.Api.Models.Settings
{
    public class CosmosDbSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
    }
}
