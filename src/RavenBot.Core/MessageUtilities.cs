namespace RavenBot.Core
{
    public static class MessageUtilities
    {
        private const string TagsStart = "%[tags:";
        private const string TagsEnd = "]%";
        private const char TagsSeparator = ';';

        private const string CategoryStart = "%[category:";
        private const string CategoryEnd = "]%";

        public static string Meta(string category, params string[] tags)
        {
            return Category(category) + Tags(tags);
        }

        public static string Tags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return string.Empty;
            return TagsStart + string.Join(TagsSeparator, tags) + TagsEnd;
        }

        public static string Category(string category)
        {
            return CategoryStart + category + CategoryEnd;
        }

        public static string TryExtractTags(string format, out string[] tags)
        {
            tags = new string[0];
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            var start = format.IndexOf(TagsStart);
            // format MUST start with this.
            // or it will be ignored.
            if (start != 0) return format;

            var tagsString = format.Split(TagsEnd)[0].Substring(TagsStart.Length);

            tags = tagsString.Split(TagsSeparator);

            return format.Replace(TagsStart + tagsString + TagsEnd, "");
        }

        public static string TryExtractCategory(string format, out string category)
        {
            category = string.Empty;

            if (string.IsNullOrEmpty(format))
                return string.Empty;

            var start = format.IndexOf(CategoryStart);
            // format MUST start with this.
            // or it will be ignored.
            if (start != 0) return format;

            category = format.Split(CategoryEnd)[0].Substring(CategoryStart.Length);

            return format.Replace(CategoryStart + category + CategoryEnd, "");
        }
    }
}
