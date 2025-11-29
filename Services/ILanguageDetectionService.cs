using google_reviews.Models;

namespace google_reviews.Services
{
    public interface ILanguageDetectionService
    {
        Language DetectLanguage(string text);
    }
}
