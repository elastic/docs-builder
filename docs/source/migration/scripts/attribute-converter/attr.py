import re
import yaml

def resolve_substitutions(subs, value):
    """
    Resolves nested attributes within a value.
    Replaces {VAR-NAME} with the corresponding value from subs.
    If VAR-NAME is not found, replaces it with {{VAR-NAME}}.
    """
    # Pattern to find all instances of {var-name} including those in URLs
    pattern = re.compile(r'\{([\w-]+)\}')

    # Replace each variable in the text with its corresponding value
    def replacement(match):
        var_name = match.group(1)
        if var_name in subs:
            return subs[var_name]
        else:
            # Enclose unresolved variables in double curly braces
            return f'{{{{{var_name}}}}}'

    # Apply substitution to the value
    return pattern.sub(replacement, value)

def parse_asciidoc(file_path):
    """
    Parses the AsciiDoc file and extracts key-value pairs.
    Handles lines in the format :key: value.
    """
    subs = {}
    pattern = re.compile(r'^:([\w-]+):\s+(.+)$')

    with open(file_path, 'r') as file:
        lines = file.readlines()

    for line in lines:
        match = pattern.match(line)
        if match:
            key = match.group(1).strip()
            value = match.group(2).strip()
            subs[key] = value

    # Resolve substitutions for all keys
    resolved_subs = {key: resolve_substitutions(subs, value) for key, value in subs.items()}
    return resolved_subs

def write_yaml(subs, output_file):
    """
    Writes the substitutions dictionary to a YAML file under the 'subs' key.
    """
    with open(output_file, 'w') as file:
        yaml.dump({'subs': subs}, file, default_flow_style=False, sort_keys=False, allow_unicode=True)

if __name__ == "__main__":
    input_file = 'attributes.asciidoc'
    output_file = 'attributes.yml'
    subs = parse_asciidoc(input_file)
    write_yaml(subs, output_file)
    print(f'Output written to {output_file}')
