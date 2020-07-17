namespace PSNotes.StatsProcessor.Models.Settings
{
    public class EventHubsSettings
    {
        public string ConnectionString { get; set; }
        public string EventHubName { get; set; }
        public string StorageAccountKey { get; set; }
        public string StorageAccountName { get; set; }
        public string StorageContainerName { get; set; }

        public string StorageConnectionString {
            get
            {
                return string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net", StorageAccountName, StorageAccountKey);
            }
        }
    }
}
