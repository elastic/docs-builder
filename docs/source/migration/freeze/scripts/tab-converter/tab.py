import os
from pathlib import Path
import re
import argparse

def extract_tab_info(html_content):
    # Extract tab titles
    button_pattern = r'<button[^>]*>\s*(.*?)\s*</button>'
    titles = re.findall(button_pattern, html_content, re.DOTALL)

    # Extract tab content
    panel_pattern = r'<div[^>]*role="tabpanel"[^>]*>(.*?)</div>'
    contents = re.findall(panel_pattern, html_content, re.DOTALL)

    # Clean up the content (remove extra whitespace)
    titles = [title.strip() for title in titles]
    contents = [content.strip() for content in contents]

    return list(zip(titles, contents))

def html_to_md_tabs(html_content):
    # Get tab information
    tabs = extract_tab_info(html_content)

    # Generate markdown output
    md_output = '::::{tab-set}\n\n'

    for title, content in tabs:
        md_output += f":::{{tab-item}} {title}\n"  # Fixed: Added curly braces
        md_output += f"{content}\n"
        md_output += ":::\n\n"

    md_output += "::::"

    return md_output

def convert_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        content = file.read()

    # Find tab sections
    tab_pattern = r'<div class="tabs"[^>]*>.*?</div>\s*</div>'
    matches = re.findall(tab_pattern, content, re.DOTALL)

    if matches:
        for match in matches:
            md_tabs = html_to_md_tabs(match)
            content = content.replace(match, md_tabs)

        with open(file_path, 'w', encoding='utf-8') as file:
            file.write(content)
        print(f"Converted HTML tabs in '{file_path}' to Markdown.")

def main():
    parser = argparse.ArgumentParser(description='Convert HTML tabs to Markdown in .md files.')
    parser.add_argument('-dir', type=str, required=True, help='Path to the directory containing Markdown files.')
    args = parser.parse_args()

    # Walk through all .md files in the directory
    for md_file in Path(args.dir).rglob('*.md'):
        convert_file(md_file)

if __name__ == "__main__":
    main()
