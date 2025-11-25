# Customer Email Campaign - Excel Template

## Required Excel Format

Your Excel file should have the following columns (in this order):

| Column | Name | Required | Description |
|--------|------|----------|-------------|
| A | Company Name | ✅ Yes | The name of the company |
| B | Email | ✅ Yes | Email address to send to |
| C | Google Maps URL | ✅ Yes | Link to their Google Maps profile |
| D | Bad Review Count | ❌ Optional | Number of low-rated reviews (defaults to 1) |
| E | Average Rating | ❌ Optional | Their average rating |

## Example Excel Content

```
Company Name          | Email                    | Google Maps URL                           | Bad Review Count | Avg Rating
Restaurang ABC       | info@restaurangabc.se     | https://maps.google.com/place/abc123      | 2               | 3.8
Café Södermalm      | kontakt@cafe.se           | https://maps.google.com/place/def456      | 1               | 4.2
Hotel Grand          | info@hotelgrand.se        | https://maps.google.com/place/ghi789      | 3               | 3.5
```

## How to Use

1. **Navigate to**: Reviews → 📧 Email Customers (Admin only)

2. **Upload** your Excel file (.xlsx or .xls)

3. **Configure** sender information:
   - Sender Name (e.g., "Adam")
   - Phone Number (e.g., "070-123 45 67")
   - Website (e.g., "https://deletify.se")

4. **Test Mode** (HIGHLY RECOMMENDED):
   - ✅ Check "Test Mode"
   - Enter your email address
   - All emails will be sent to YOU instead of customers
   - Review the email formatting and content
   - Make sure everything looks perfect

5. **Live Mode** (After testing):
   - ⬜ Uncheck "Test Mode"
   - Click "Send to All Customers"
   - Emails will be sent to real customer addresses

## Email Content

The system will generate a beautifully formatted email with:

- Professional Deletify branding
- Personalized company name
- Link to their Google Maps profile
- Number of bad reviews found
- Clean bullet points with benefits
- Call-to-action section
- Your contact information
- PS section with booking option

## Safety Features

- ✅ Email validation (invalid emails are skipped)
- ✅ Test mode to preview emails safely
- ✅ Real-time progress tracking
- ✅ 1-second delay between emails (prevents spam flags)
- ✅ Success/failure reporting

## Tips

- Always use **Test Mode** first!
- Keep your Excel file simple and clean
- Remove any duplicate email addresses
- Make sure Google Maps URLs are complete
- Test with 2-3 customers first before sending to all
