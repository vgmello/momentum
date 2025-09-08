#!/bin/bash

# k6 Performance Test Runner Script
# Usage: ./run-tests.sh [environment] [test-scenario]
# Example: ./run-tests.sh local cashiers/baseline
#          ./run-tests.sh staging mixed/realistic-workflow

set -e

# Default values
ENVIRONMENT="${1:-local}"
TEST_SCENARIO="${2:-cashiers/baseline}"

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
    print_error "k6 is not installed. Please install k6 first."
    echo "Installation instructions: https://k6.io/docs/getting-started/installation/"
    exit 1
fi

# Set environment variables based on environment
case $ENVIRONMENT in
    local)
        export API_BASE_URL="${API_BASE_URL:-http://localhost:8101}"
        export GRPC_ENDPOINT="${GRPC_ENDPOINT:-localhost:8102}"
        ;;
    staging)
        export API_BASE_URL="${API_BASE_URL:-http://staging-api:8101}"
        export GRPC_ENDPOINT="${GRPC_ENDPOINT:-staging-api:8102}"
        ;;
    production)
        print_error "Production testing not allowed from this script for safety reasons."
        exit 1
        ;;
    *)
        print_error "Unknown environment: $ENVIRONMENT"
        echo "Valid environments: local, staging"
        exit 1
        ;;
esac

# Set common environment variables
export ENVIRONMENT="$ENVIRONMENT"
export K6_WEB_DASHBOARD="${K6_WEB_DASHBOARD:-true}"
export K6_WEB_DASHBOARD_PORT="${K6_WEB_DASHBOARD_PORT:-5665}"

# Create results directory if it doesn't exist
RESULTS_DIR="../../results"
mkdir -p "$RESULTS_DIR"

# Generate timestamp for result files
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
RESULT_FILE="${RESULTS_DIR}/summary_${ENVIRONMENT}_${TIMESTAMP}.json"
HTML_REPORT="${RESULTS_DIR}/report_${ENVIRONMENT}_${TIMESTAMP}.html"

# Build the test file path
TEST_FILE="scenarios/${TEST_SCENARIO}.js"

# Check if test file exists
if [ ! -f "$TEST_FILE" ]; then
    print_error "Test file not found: $TEST_FILE"
    echo "Available tests:"
    find scenarios -name "*.js" -type f | sed 's|scenarios/||' | sed 's|\.js||'
    exit 1
fi

# Print test configuration
print_info "Starting k6 Performance Test"
echo "Environment: $ENVIRONMENT"
echo "Test Scenario: $TEST_SCENARIO"
echo "API URL: $API_BASE_URL"
echo "Results: $RESULT_FILE"
echo ""

# Build k6 command
K6_CMD="k6 run"

# Add web dashboard if enabled
if [ "$K6_WEB_DASHBOARD" = "true" ]; then
    K6_CMD="$K6_CMD --web-dashboard --web-dashboard-port=$K6_WEB_DASHBOARD_PORT"
    print_info "Web Dashboard will be available at http://localhost:${K6_WEB_DASHBOARD_PORT}"
fi

# Add output options
K6_CMD="$K6_CMD --summary-export=$RESULT_FILE"

# Add the test file
K6_CMD="$K6_CMD $TEST_FILE"

# Run the test
print_info "Executing: $K6_CMD"
echo ""

if $K6_CMD; then
    print_info "Test completed successfully!"
    echo ""
    print_info "Results saved to: $RESULT_FILE"
    
    # Show summary if jq is available
    if command -v jq &> /dev/null; then
        echo ""
        print_info "Test Summary:"
        jq -r '.metrics | to_entries[] | "\(.key): \(.value.avg // .value.value // .value.count)"' "$RESULT_FILE" | head -10
    fi
else
    print_error "Test failed!"
    exit 1
fi

echo ""
print_info "Done!"