using System;
using Toggl.Multivac;

namespace Toggl.Foundation.Autocomplete.Suggestions
{
    public sealed class NoEntityInfoMessage : AutocompleteSuggestion
    {
        private const string tagIconIdentifier = "icIllustrationTagsSmall";
        private const string projectIconIdentifier = "icIllustrationProjectsSmall";

        public string Text { get; }
        public string ImageResource { get; }
        public char? CharacterToReplace { get; }

        public NoEntityInfoMessage(
            string text, string imageResource, char? characterToReplace)
        {
            Ensure.Argument.IsNotNull(text, nameof(text));

            if (!string.IsNullOrEmpty(imageResource) && characterToReplace == null)
                throw new ArgumentNullException($"{nameof(characterToReplace)} must not be null, when {nameof(imageResource)} is set");

            Text = text;
            ImageResource = imageResource;
            CharacterToReplace = characterToReplace;
        }

        public override int GetHashCode()
        {
            return HashCode.From(Text, ImageResource);
        }

        public static NoEntityInfoMessage CreateTag()
        {
            return new NoEntityInfoMessage(
                text: Resources.NoTagsInfoMessage,
                imageResource: tagIconIdentifier,
                characterToReplace: '#');
        }

        public static NoEntityInfoMessage CreateProject()
        {
            return new NoEntityInfoMessage(
                text: Resources.NoProjectsInfoMessage,
                imageResource: projectIconIdentifier,
                characterToReplace: '@');
        }
    }
}
