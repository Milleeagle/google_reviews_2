#!/usr/bin/env python3
import ftplib
import os
import sys
from pathlib import Path

FTP_HOST = "oloflarssonnymans.nu"
FTP_USER = "s018430b"
FTP_PASS = "T9v!pQ7mZ2#rL4xA"
PUBLISH_DIR = "./publish"

def delete_ftp_recursive(ftp, path=""):
    """Recursively delete everything in the FTP directory"""
    try:
        items = []
        ftp.retrlines(f'NLST {path}', items.append)

        for item in items:
            item_name = item.split('/')[-1]
            full_path = f"{path}/{item_name}" if path else item_name

            # Try to delete as file
            try:
                ftp.delete(full_path)
                print(f"✓ Deleted file: {full_path}")
            except:
                # Must be a directory, recurse
                try:
                    delete_ftp_recursive(ftp, full_path)
                    ftp.rmd(full_path)
                    print(f"✓ Deleted directory: {full_path}")
                except Exception as e:
                    print(f"✗ Could not delete {full_path}: {e}")
    except:
        pass

def upload_directory(ftp, local_dir, remote_dir=""):
    """Recursively upload directory contents"""
    uploaded = 0
    failed = 0

    for root, dirs, files in os.walk(local_dir):
        # Calculate relative path
        rel_path = os.path.relpath(root, local_dir)
        if rel_path == ".":
            ftp_path = remote_dir
        else:
            ftp_path = os.path.join(remote_dir, rel_path).replace('\\', '/')

        # Create directories
        if ftp_path:
            parts = ftp_path.split('/')
            current = ""
            for part in parts:
                current = f"{current}/{part}" if current else part
                try:
                    ftp.mkd(current)
                except:
                    pass  # Directory might already exist

        # Upload files
        for filename in files:
            local_path = os.path.join(root, filename)
            remote_path = os.path.join(ftp_path, filename).replace('\\', '/') if ftp_path else filename

            try:
                with open(local_path, 'rb') as f:
                    ftp.storbinary(f'STOR {remote_path}', f)
                print(f"✓ Uploaded: {remote_path}")
                uploaded += 1

                if uploaded % 10 == 0:
                    print(f"  Progress: {uploaded} files uploaded...")
            except Exception as e:
                print(f"✗ Failed to upload {remote_path}: {e}")
                failed += 1

    return uploaded, failed

def main():
    print("=" * 50)
    print("  Automated FTP Deployment")
    print("=" * 50)
    print()

    # Check if publish directory exists
    if not os.path.exists(PUBLISH_DIR):
        print(f"ERROR: Publish directory '{PUBLISH_DIR}' not found!")
        sys.exit(1)

    try:
        # Connect to FTP
        print("Connecting to FTP server...")
        ftp = ftplib.FTP(FTP_HOST, FTP_USER, FTP_PASS, timeout=30)
        print(f"✓ Connected to {FTP_HOST}")
        print()

        # Step 1: Delete all existing files
        print("Step 1: Cleaning FTP server...")
        delete_ftp_recursive(ftp)
        print("✓ FTP server cleaned")
        print()

        # Step 2: Upload new files
        print(f"Step 2: Uploading files from {PUBLISH_DIR}...")
        uploaded, failed = upload_directory(ftp, PUBLISH_DIR)
        print()

        # Summary
        print("=" * 50)
        print("Deployment Summary:")
        print(f"  Successfully uploaded: {uploaded}")
        print(f"  Failed uploads: {failed}")
        print("=" * 50)

        if failed == 0:
            print("🎉 Deployment completed successfully!")
        else:
            print(f"⚠️  Deployment completed with {failed} errors.")

        ftp.quit()

    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
