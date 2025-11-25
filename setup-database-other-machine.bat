@echo off
echo Setting up database on new machine using Entity Framework migrations...
echo.

echo Step 1: Restoring NuGet packages...
dotnet restore

echo.
echo Step 2: Applying database migrations...
dotnet ef database update

if %errorlevel% equ 0 (
    echo.
    echo ✅ Database setup complete!
    echo.
    echo Note: This creates an empty database with the correct schema.
    echo If you need existing data, use the database backup method instead.
    echo.
) else (
    echo.
    echo ❌ Database setup failed. Make sure you have:
    echo   - .NET SDK installed
    echo   - Entity Framework tools: dotnet tool install --global dotnet-ef
    echo   - SQL Server LocalDB installed
    echo.
)

pause