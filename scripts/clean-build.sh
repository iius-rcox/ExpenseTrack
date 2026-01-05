#!/bin/bash
# clean-build.sh - Clean .NET build artifacts to free disk space
# Usage: ./scripts/clean-build.sh [--all]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}ExpenseFlow Build Artifact Cleanup${NC}"
echo "========================================"

# Function to get directory size
get_size() {
    du -sh "$1" 2>/dev/null | cut -f1 || echo "0"
}

# Function to clean directories and report savings
clean_dirs() {
    local pattern="$1"
    local description="$2"

    local dirs=$(find "$PROJECT_ROOT/backend" -type d -name "$pattern" 2>/dev/null)
    if [ -z "$dirs" ]; then
        echo -e "  ${GREEN}✓${NC} No $description directories found"
        return
    fi

    local total_size=$(echo "$dirs" | xargs du -ch 2>/dev/null | tail -1 | cut -f1)
    echo "$dirs" | xargs rm -rf 2>/dev/null
    echo -e "  ${GREEN}✓${NC} Cleaned $description: ${total_size} freed"
}

# Calculate initial sizes
echo ""
echo "Scanning for build artifacts..."

TESTS_BIN_SIZE=$(du -ch "$PROJECT_ROOT"/backend/tests/*/bin 2>/dev/null | tail -1 | cut -f1 || echo "0")
TESTS_OBJ_SIZE=$(du -ch "$PROJECT_ROOT"/backend/tests/*/obj 2>/dev/null | tail -1 | cut -f1 || echo "0")

echo ""
echo "Test artifacts found:"
echo "  - bin directories: $TESTS_BIN_SIZE"
echo "  - obj directories: $TESTS_OBJ_SIZE"

# Always clean test artifacts
echo ""
echo -e "${YELLOW}Cleaning test build artifacts...${NC}"
rm -rf "$PROJECT_ROOT"/backend/tests/*/bin "$PROJECT_ROOT"/backend/tests/*/obj 2>/dev/null
echo -e "  ${GREEN}✓${NC} Test bin/obj directories removed"

# Clean all if --all flag passed
if [ "$1" == "--all" ]; then
    echo ""
    echo -e "${YELLOW}Cleaning ALL build artifacts (--all mode)...${NC}"

    SRC_BIN_SIZE=$(du -ch "$PROJECT_ROOT"/backend/src/*/bin 2>/dev/null | tail -1 | cut -f1 || echo "0")
    SRC_OBJ_SIZE=$(du -ch "$PROJECT_ROOT"/backend/src/*/obj 2>/dev/null | tail -1 | cut -f1 || echo "0")

    echo "  Source artifacts: bin=$SRC_BIN_SIZE, obj=$SRC_OBJ_SIZE"

    rm -rf "$PROJECT_ROOT"/backend/src/*/bin "$PROJECT_ROOT"/backend/src/*/obj 2>/dev/null
    echo -e "  ${GREEN}✓${NC} Source bin/obj directories removed"

    # Clean TestResults if present
    if [ -d "$PROJECT_ROOT/backend/TestResults" ]; then
        rm -rf "$PROJECT_ROOT/backend/TestResults"
        echo -e "  ${GREEN}✓${NC} TestResults directory removed"
    fi
fi

# Show final state
echo ""
echo -e "${GREEN}Cleanup complete!${NC}"
echo ""
echo "Current test directory sizes:"
du -sh "$PROJECT_ROOT"/backend/tests/* 2>/dev/null | sort -hr

if [ "$1" != "--all" ]; then
    echo ""
    echo -e "${YELLOW}Tip:${NC} Run with --all to also clean source project artifacts"
    echo "     ./scripts/clean-build.sh --all"
fi
