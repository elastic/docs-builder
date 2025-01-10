import yaml
from collections import defaultdict
import os

def read_yaml(file_path):
    """Reads and parses the YAML file."""
    with open(file_path, 'r', encoding='utf-8') as file:
        return yaml.safe_load(file)

def traverse_sections(sections, repo_paths):
    """
    Recursively traverses nested sections to gather repository paths.

    Args:
        sections (list): List of section dictionaries.
        repo_paths (defaultdict): Dictionary mapping repo names to sets of paths.
    """
    for section in sections:
        # Gather sources in the current section
        sources = section.get('sources', [])
        for source in sources:
            repo_name = source.get('repo')
            path = source.get('path')
            if repo_name and path:
                # Remove trailing slashes and replace double slashes with single slashes
                clean_path = path.rstrip('/').replace('//', '/')
                # Format the path for CODEOWNERS
                formatted_path = f"{clean_path}/*.asciidoc @docs-freeze-team"
                repo_paths[repo_name].add(formatted_path)

        # If there are nested sections, traverse them recursively
        nested_sections = section.get('sections', [])
        if nested_sections:
            traverse_sections(nested_sections, repo_paths)

def gather_repo_paths(contents):
    """
    Gathers all unique paths for each repository from the contents.

    Args:
        contents (list): List of content dictionaries from YAML.

    Returns:
        defaultdict: Dictionary mapping repo names to sets of formatted paths.
    """
    repo_paths = defaultdict(set)
    for content in contents:
        sections = content.get('sections', [])
        traverse_sections(sections, repo_paths)
    return repo_paths

def generate_markdown_output(repo_paths, repos_info):
    """
    Generates Markdown output with repository headings and CODEOWNERS code blocks.

    Args:
        repo_paths (dict): Dictionary mapping repo names to sets of paths.
        repos_info (dict): Dictionary mapping repo names to their GitHub URLs.

    Returns:
        str: Formatted Markdown content as a string.
    """
    output = ""
    for repo in sorted(repo_paths.keys()):
        repo_url = repos_info.get(repo, '#')  # Default to '#' if URL not found
        if repo_url.endswith('.git'):
            repo_url = repo_url[:-4]  # Remove the '.git' suffix for proper URL

        # Create level three heading with repository link
        output += f"### [{repo}]({repo_url})\n\n"

        # Start code block
        output += "```\n"

        # Add each CODEOWNERS line within the code block
        for path in sorted(repo_paths[repo]):
            output += f"{path}\n"

        # End code block
        output += "```\n\n"

    return output

def main():
    # Define the path to the 'conf.yaml' file (assumes it's in the same directory)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    file_path = os.path.join(script_dir, 'conf.yaml')

    # Read and parse the YAML file
    yaml_content = read_yaml(file_path)

    # Extract repository information
    repos_info = yaml_content.get('repos', {})

    # Extract contents
    contents = yaml_content.get('contents', [])

    # Gather all repository paths
    repo_paths = gather_repo_paths(contents)

    # Generate the Markdown content
    markdown_output = generate_markdown_output(repo_paths, repos_info)

    # Define the output Markdown file path
    output_path = os.path.join(script_dir, 'output.md')

    # Write the Markdown content to the file
    with open(output_path, 'w', encoding='utf-8') as output_file:
        output_file.write(markdown_output)

    print(f"Markdown content has been written to '{output_path}'")

if __name__ == "__main__":
    main()
