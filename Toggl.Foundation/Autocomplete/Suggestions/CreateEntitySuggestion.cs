namespace Toggl.Foundation.Autocomplete.Suggestions
{
    public sealed class CreateEntitySuggestion : AutocompleteSuggestion
    {
        public string CreateEntityMessage { get; }

        public string EntityName { get; }

        public CreateEntitySuggestion(string createEntityPrefix, string entityName)
        {
            EntityName = entityName;
            CreateEntityMessage = $"{createEntityPrefix} \"{entityName}\"";
        }

        public override int GetHashCode()
            => CreateEntityMessage.GetHashCode();
    }
}
