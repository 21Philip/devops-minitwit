#!/bin/bash
# Create a new release for ITU-MiniTwit
# Usage: ./release.sh v1.0.0 "Release description"

set -e

if [ $# -lt 1 ]; then
    echo "Usage: $0 <version> [description]"
    echo "Example: $0 v1.0.0 'Initial production release'"
    exit 1
fi

VERSION=$1
DESCRIPTION=${2:-"Release $VERSION"}

echo "Creating release: $VERSION"
echo "Description: $DESCRIPTION"
echo ""

# Check if tag already exists
if git rev-parse "$VERSION" >/dev/null 2>&1; then
    echo "Tag $VERSION already exists!"
    exit 1
fi

# Create annotated tag
git tag -a "$VERSION" -m "$DESCRIPTION"

echo "Tag created: $VERSION"
echo ""
echo "To push the release:"
echo "  git push origin $VERSION"
echo ""
echo "To list all releases:"
echo "  git tag -l"

