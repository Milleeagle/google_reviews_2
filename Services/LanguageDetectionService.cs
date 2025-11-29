using google_reviews.Models;
using System.Text.RegularExpressions;

namespace google_reviews.Services
{
    public class LanguageDetectionService : ILanguageDetectionService
    {
        private readonly HashSet<string> _swedishWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "och", "det", "att", "i", "en", "jag", "hon", "som", "han", "på", "den", "med", "var", "sig", "för",
            "så", "till", "är", "men", "ett", "om", "hade", "de", "av", "icke", "mig", "du", "henne", "då", "sin",
            "nu", "har", "inte", "hans", "honom", "skulle", "hennes", "där", "min", "man", "ej", "vid", "kunde",
            "något", "från", "ut", "när", "efter", "upp", "vi", "dem", "vara", "vad", "över", "än", "dig", "kan",
            "sina", "här", "ha", "mot", "alla", "under", "någon", "eller", "allt", "mycket", "sedan", "ju", "denna",
            "själv", "detta", "åt", "utan", "varit", "hur", "ingen", "mitt", "ni", "bli", "blev", "oss", "din",
            "dessa", "några", "deras", "blir", "mina", "samma", "vilken", "er", "sådan", "vår", "blivit", "dess",
            "inom", "mellan", "sådant", "varför", "varje", "vilka", "ditt", "vem", "vilket", "sitta", "sådana",
            "vart", "dina", "vars", "vårt", "våra", "ert", "era", "vilkas",
            // Additional Swedish-specific words
            "företag", "tjänst", "service", "kund", "recension", "betyg", "kontakt", "information",
            "svensk", "svenska", "sverige", "hej", "tack", "välkommen", "bra", "dålig", "bättre", "sämre"
        };

        private readonly HashSet<string> _englishWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i", "it", "for", "not", "on", "with",
            "he", "as", "you", "do", "at", "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
            "or", "an", "will", "my", "one", "all", "would", "there", "their", "what", "so", "up", "out", "if",
            "about", "who", "get", "which", "go", "me", "when", "make", "can", "like", "time", "no", "just", "him",
            "know", "take", "people", "into", "year", "your", "good", "some", "could", "them", "see", "other", "than",
            "then", "now", "look", "only", "come", "its", "over", "think", "also", "back", "after", "use", "two",
            "how", "our", "work", "first", "well", "way", "even", "new", "want", "because", "any", "these", "give",
            "day", "most", "us",
            // Additional English-specific words
            "company", "service", "customer", "review", "rating", "contact", "information",
            "english", "welcome", "hello", "thanks", "thank", "good", "bad", "better", "worse"
        };

        public Language DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Language.Swedish; // Default to Swedish

            // Normalize text (lowercase, remove special chars except Swedish chars)
            var normalizedText = text.ToLower();

            // Strong indicator: Check for Swedish-specific characters (å, ä, ö)
            int swedishScore = 0;
            int englishScore = 0;

            // Swedish characters are a strong indicator
            if (normalizedText.Contains('å')) swedishScore += 3;
            if (normalizedText.Contains('ä')) swedishScore += 3;
            if (normalizedText.Contains('ö')) swedishScore += 3;

            // Extract words (alphanumeric sequences)
            var words = Regex.Matches(normalizedText, @"\b[a-zåäöæø]+\b", RegexOptions.IgnoreCase)
                             .Cast<Match>()
                             .Select(m => m.Value)
                             .Where(w => w.Length >= 2); // Ignore single-letter words

            foreach (var word in words)
            {
                var cleanWord = word.Trim();
                if (_swedishWords.Contains(cleanWord))
                {
                    swedishScore++;
                }
                if (_englishWords.Contains(cleanWord))
                {
                    englishScore++;
                }
            }

            // Return language with higher score, defaulting to Swedish on tie
            return swedishScore >= englishScore ? Language.Swedish : Language.English;
        }
    }
}
