using google_reviews.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace google_reviews.Services;

public class GoogleReviewScraper : IReviewScraper, IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly ILogger<GoogleReviewScraper> _logger;
    private readonly ChromeDriverService _service;
    private readonly int _serviceProcessId;
    private bool _disposed = false;

    public GoogleReviewScraper(ILogger<GoogleReviewScraper> logger)
    {
        _logger = logger;

        var options = new ChromeOptions();
        options.AddArguments(
            "--headless",
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--window-size=1920,1080",
            "--disable-blink-features=AutomationControlled",
            "--lang=en-US",
            "--disable-extensions",
            "--disable-plugins",
            "--force-device-scale-factor=1"
        );

        // Create ChromeDriverService to have more control over the process
        _service = ChromeDriverService.CreateDefaultService();
        _service.HideCommandPromptWindow = true;
        _service.SuppressInitialDiagnosticInformation = true;

        _driver = new ChromeDriver(_service, options);
        _serviceProcessId = _service.ProcessId;

        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        _logger.LogDebug("Created ChromeDriver with service PID: {ProcessId}", _serviceProcessId);
    }

    public async Task<List<Review>> ScrapeReviewsAsync(string placeId)
    {
        var reviews = new List<Review>();

        // Skip known problematic place IDs
        if (placeId == "ChIJiQDEAHedX0YRNE9wzvvLI38")
        {
            _logger.LogWarning("Skipping known problematic Place ID: {PlaceId}", placeId);
            return reviews;
        }

        try
        {
            var googleMapsUrl = $"https://www.google.com/maps/place/?q=place_id:{placeId}";
            _logger.LogInformation("Scraping reviews for Place ID: {PlaceId}", placeId);
            _driver.Navigate().GoToUrl(googleMapsUrl);

            // Wait for page load
            try
            {
                _wait.Until(d => d.FindElement(By.TagName("body")));
            }
            catch { }
            await Task.Delay(1500);

            // Handle consent dialog
            await HandleConsentDialog();

            // Click Reviews tab
            await ClickReviewsTab();
            await Task.Delay(2500); // Increased wait time for reviews to load

            // Scroll to load all reviews (up to 100)
            await ScrollReviews(100);

            // Extract reviews
            reviews = ExtractAllReviews(100);

            _logger.LogInformation("Scraped {Count} reviews for Place ID: {PlaceId}", reviews.Count, placeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping reviews for Place ID: {PlaceId}", placeId);
        }

        return reviews;
    }

    private async Task HandleConsentDialog()
    {
        try
        {
            var consentSelectors = new[]
            {
                "//button[contains(., 'Accept')]",
                "//button[contains(., 'Reject')]",
                "//button[contains(., 'Godkänn')]",
                "//button[contains(., 'Avvisa')]",
                "//button[@aria-label='Accept all']",
                "//button[@aria-label='Reject all']",
                "//form[@action]//button[2]",
                "//button[contains(@class, 'VfPpkd-LgbsSe')]"
            };

            foreach (var selector in consentSelectors)
            {
                try
                {
                    var button = _driver.FindElement(By.XPath(selector));
                    if (button != null && button.Displayed)
                    {
                        button.Click();
                        await Task.Delay(1000);
                        _logger.LogInformation("Clicked consent button");
                        break;
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not handle consent dialog: {Message}", ex.Message);
        }
    }

    private async Task ClickReviewsTab()
    {
        try
        {
            var selectors = new[]
            {
                "button[aria-label*='Review']",
                "button[role='tab'][aria-label*='Review']",
                "button[data-tab-index='1']"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var tab = _driver.FindElement(By.CssSelector(selector));
                    if (tab.Displayed)
                    {
                        tab.Click();
                        _logger.LogInformation("Clicked Reviews tab");
                        return;
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not click Reviews tab: {Message}", ex.Message);
        }
    }

    private async Task ScrollReviews(int targetCount)
    {
        try
        {
            var scrollableDiv = _driver.FindElement(By.CssSelector("div[role='main']"));
            int scrollCount = Math.Min((targetCount / 5) + 1, 4);

            for (int i = 0; i < scrollCount; i++)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].scrollBy(0, 1000);", scrollableDiv);
                await Task.Delay(500); // Increased from 200ms
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error scrolling: {Message}", ex.Message);
        }
    }

    private List<Review> ExtractAllReviews(int maxReviews)
    {
        var reviews = new List<Review>();
        var uniqueReviews = new HashSet<string>();

        try
        {
            var selectors = new[]
            {
                "div[data-review-id]",
                ".jftiEf",
                "div.jftiEf",
                "[jsaction*='review']",
                "div[data-review-id], .jftiEf"
            };

            IReadOnlyCollection<IWebElement> reviewElements = new List<IWebElement>();

            foreach (var selector in selectors)
            {
                try
                {
                    reviewElements = _driver.FindElements(By.CssSelector(selector));
                    if (reviewElements.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} review elements", reviewElements.Count);
                        break;
                    }
                }
                catch { }
            }

            if (reviewElements.Count == 0)
            {
                _logger.LogWarning("No review elements found with any selector");

                // Debug logging without storing large page source
                try
                {
                    _logger.LogWarning("Page title: {Title}", _driver.Title);
                    _logger.LogWarning("Current URL: {Url}", _driver.Url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not get page info: {Message}", ex.Message);
                }

                return reviews;
            }

            foreach (var element in reviewElements.Take(maxReviews * 2))
            {
                try
                {
                    var review = ExtractReview(element);
                    if (review != null)
                    {
                        var uniqueKey = $"{review.AuthorName}_{review.Text}";
                        if (uniqueReviews.Add(uniqueKey) && !string.IsNullOrEmpty(review.AuthorName))
                        {
                            reviews.Add(review);
                            if (reviews.Count >= maxReviews)
                                break;
                        }
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting reviews");
        }

        return reviews;
    }

    private Review? ExtractReview(IWebElement element)
    {
        try
        {
            var review = new Review
            {
                Id = Guid.NewGuid().ToString()
            };

            // Extract author name
            try
            {
                var authorElement = element.FindElement(By.CssSelector(".d4r55"));
                review.AuthorName = authorElement.Text.Trim();
            }
            catch { review.AuthorName = "Anonymous"; }

            // Extract rating
            try
            {
                var ratingElement = element.FindElement(By.CssSelector(".kvMYJc"));
                var ariaLabel = ratingElement.GetAttribute("aria-label");

                var patterns = new[]
                {
                    @"(\d+)\s*star",
                    @"(\d+)\s*stjärn",
                    @"(\d+)\s*étoile",
                    @"(\d+)\s*Stern"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(ariaLabel, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        review.Rating = int.Parse(match.Groups[1].Value);
                        break;
                    }
                }
            }
            catch { review.Rating = 0; }

            // Extract review text
            try
            {
                var textElement = element.FindElement(By.CssSelector(".wiI7pd"));
                review.Text = textElement.Text.Trim();
            }
            catch { review.Text = ""; }

            // Extract time
            try
            {
                var timeElement = element.FindElement(By.CssSelector(".rsqaWe"));
                var relativeTime = timeElement.Text.Trim();
                review.Time = ParseRelativeTime(relativeTime);
            }
            catch
            {
                review.Time = DateTime.UtcNow;
            }

            // Extract author URL and photo
            try
            {
                var authorLink = element.FindElement(By.CssSelector(".WNxzHc a"));
                review.AuthorUrl = authorLink.GetAttribute("href") ?? "";
            }
            catch { review.AuthorUrl = ""; }

            try
            {
                var photoElement = element.FindElement(By.CssSelector(".NBa7we"));
                review.ProfilePhotoUrl = photoElement.GetAttribute("src") ?? "";
            }
            catch { review.ProfilePhotoUrl = ""; }

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Failed to extract review: {Message}", ex.Message);
            return null;
        }
    }

    private DateTime ParseRelativeTime(string relativeTime)
    {
        var now = DateTime.UtcNow;
        var lowerTime = relativeTime.ToLower();

        try
        {
            // Handle "a [unit] ago" / "an [unit] ago" / "ett [unit] sedan" (e.g., "a month ago", "ett år sedan")
            if (Regex.IsMatch(lowerTime, @"\b(a|an|en|ett)\s+(second|sekund|minute|minut|hour|timme|day|dag|week|vecka|month|månad|year|år)", RegexOptions.IgnoreCase))
            {
                var result = now;
                if (lowerTime.Contains("second") || lowerTime.Contains("sekund")) result = now.AddSeconds(-1);
                else if (lowerTime.Contains("minute") || lowerTime.Contains("minut")) result = now.AddMinutes(-1);
                else if (lowerTime.Contains("hour") || lowerTime.Contains("timme")) result = now.AddHours(-1);
                else if (lowerTime.Contains("day") || lowerTime.Contains("dag")) result = now.AddDays(-1);
                else if (lowerTime.Contains("week") || lowerTime.Contains("vecka")) result = now.AddDays(-7);
                else if (lowerTime.Contains("month") || lowerTime.Contains("månad")) result = now.AddMonths(-1);
                else if (lowerTime.Contains("year") || lowerTime.Contains("år")) result = now.AddYears(-1);

                return result;
            }

            // Pattern: "X [unit] ago" or "för X [unit] sedan" (Swedish)
            // Support both singular and plural: year/years/år, month/months/månad/månader, etc.
            // Match variations: "3 months ago", "för 3 månader sedan", "3 days ago"
            var match = Regex.Match(lowerTime, @"(\d+)\s*(second|sekund|minute|minut|hour|timme|timmar|day|dag|dagar|week|vecka|veckor|month|månad|månader|year|år)s?\b", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var value = int.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value.ToLower();

                return unit switch
                {
                    var u when u.Contains("second") || u.Contains("sekund") => now.AddSeconds(-value),
                    var u when u.Contains("minute") || u.Contains("minut") => now.AddMinutes(-value),
                    var u when u.Contains("hour") || u.Contains("timme") || u.Contains("timmar") => now.AddHours(-value),
                    var u when u.Contains("day") || u.Contains("dag") => now.AddDays(-value),
                    var u when u.Contains("week") || u.Contains("vecka") || u.Contains("veckor") => now.AddDays(-value * 7),
                    var u when u.Contains("month") || u.Contains("månad") || u.Contains("månader") => now.AddMonths(-value),
                    var u when u.Contains("year") || u.Contains("år") => now.AddYears(-value),
                    _ => now
                };
            }
        }
        catch
        {
            // Failed to parse, return current time
        }

        return now;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing ChromeDriver with service PID: {ProcessId}", _serviceProcessId);

            try
            {
                // Quit the driver first (this should close browser and stop service)
                _driver?.Quit();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error quitting driver: {Message}", ex.Message);
            }

            try
            {
                // Dispose the driver object
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error disposing driver: {Message}", ex.Message);
            }

            // Force kill the ChromeDriver service process if it's still running
            try
            {
                var serviceProcess = Process.GetProcessById(_serviceProcessId);
                if (!serviceProcess.HasExited)
                {
                    _logger.LogDebug("ChromeDriver service {ProcessId} still running, killing it", _serviceProcessId);
                    serviceProcess.Kill(true); // true = kill entire process tree
                    serviceProcess.WaitForExit(3000); // Wait up to 3 seconds
                    _logger.LogDebug("Killed ChromeDriver service {ProcessId}", _serviceProcessId);
                }
            }
            catch (ArgumentException)
            {
                // Process already exited - this is fine
                _logger.LogDebug("ChromeDriver service {ProcessId} already exited", _serviceProcessId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error killing ChromeDriver service {ProcessId}: {Message}", _serviceProcessId, ex.Message);
            }

            try
            {
                // Dispose the service
                _service?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error disposing service: {Message}", ex.Message);
            }

            // Small delay to ensure cleanup completes
            Thread.Sleep(500);

            _disposed = true;
            _logger.LogDebug("ChromeDriver disposal complete for service PID: {ProcessId}", _serviceProcessId);
        }
    }
}
