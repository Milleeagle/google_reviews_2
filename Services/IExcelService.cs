using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IExcelService
    {
        byte[] GenerateReviewReportExcel(ReviewMonitorReport report);
        List<Company> ImportCompaniesFromExcel(Stream fileStream);
    }
}