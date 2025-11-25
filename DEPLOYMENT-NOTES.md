# Deployment Notes

## Working Deployment Process

**Issue Discovered:** The PowerShell FTP deployment scripts were not working correctly for updating the live application.

**Solution:** Manual deployment using Visual Studio publish + manual FTP upload works correctly.

### Working Deployment Steps:

1. **Build in Visual Studio:**
   - Use Visual Studio's "Publish" feature
   - This creates a proper publish folder with all dependencies

2. **Manual FTP Upload:**
   - Upload the contents of the publish folder manually via FTP
   - This ensures all files are properly uploaded and the application restarts correctly

### Problems with Automated Scripts:

- `deploy-to-ftp.ps1` and related PowerShell scripts were uploading files but the application wasn't picking up the changes
- Possible issues:
  - Files not being uploaded to correct location
  - Application not restarting properly after upload
  - Missing dependencies or incorrect file structure

### Recommendation:

For reliable deployments, use the manual Visual Studio publish + FTP upload process until the automated scripts can be debugged and fixed.

### Status:

- ✅ Manual deployment: **WORKING**
- ❌ Automated PowerShell scripts: **NOT WORKING**
- ✅ Application now running with latest code
- ✅ Developer exception page enabled for debugging

---
*Updated: 2025-09-15*