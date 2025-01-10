---
title: HTML to Markdown Tab Converter
---

### HTML to Markdown Tab Converter

The tab converter script is used to convert HTML tabs into Markdown tabs within documentation files. The script scans through specified directories, finds all Markdown (`.md`) files, and replaces any HTML-based tab structures with their Markdown equivalents.

**Note**: Running this script may or may not be necessary depending on the evolution of our migration tool. Use it as needed based on the current state of the documentation.

#### How to Run the Script

To execute the script and convert HTML tabs to Markdown tabs, follow these steps:

1. **Navigate to the Script Directory**:

   From the root of the repository, navigate to the tab converter script directory:

   ```sh
   cd docs/source/migration/scripts/tab-converter
   ```

2. **Run the Script**:

   Execute the script by pointing it to the directory containing the Markdown files you wish to process. Replace `/path/to/markdown/files` with the actual path to your Markdown files.

   ```sh
   python3 tab_converter.py /path/to/markdown/files
   ```

   **Example**:

   ```sh
   python3 tab_converter.py ../../../content/docs/
   ```

   This command processes all `.md` files within the `content/docs/` directory relative to the script's location.

#### Important Notes

- **Backup Your Files**: Before running the script, it's recommended to back up your Markdown files or ensure they are under version control (e.g., in a Git repository). This allows you to revert changes if necessary.
- **Verify Changes**: After running the script, review the modified files to ensure that the tabs have been converted correctly.