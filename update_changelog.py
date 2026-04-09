import re

changelog_path = 'Toris/Assets/Documentation/Changelog/CHANGELOG.md'

with open(changelog_path, 'r') as f:
    content = f.read()

# Fix the duplicate header
content = content.replace("## [Current/Recent] - Implemented Skills Screen UI Framework\n## [Current/Recent] - Implemented Skills Screen UI Framework", "## [Current/Recent] - Implemented Skills Screen UI Framework")

with open(changelog_path, 'w') as f:
    f.write(content)
