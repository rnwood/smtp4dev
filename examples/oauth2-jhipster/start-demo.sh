#!/bin/bash
# Quick start script for OAuth2/XOAUTH2 demo

set -e

echo "========================================================================"
echo "OAuth2/XOAUTH2 Authentication Demo with JHipster Registry"
echo "========================================================================"
echo ""

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null && ! command -v docker &> /dev/null; then
    echo "Error: Docker and Docker Compose are required"
    echo "Please install Docker Desktop or Docker Engine + Docker Compose"
    exit 1
fi

# Use docker-compose or docker compose based on availability
DOCKER_COMPOSE="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
fi

echo "Step 1: Starting services..."
echo "----------------------------"
$DOCKER_COMPOSE up -d

echo ""
echo "Step 2: Waiting for services to be ready..."
echo "-------------------------------------------"

# Wait for JHipster Registry
echo -n "Waiting for JHipster Registry (this may take 30-60 seconds)..."
attempts=0
max_attempts=60
while ! curl -sf http://localhost:8761/management/health > /dev/null 2>&1; do
    if [ $attempts -ge $max_attempts ]; then
        echo " TIMEOUT!"
        echo ""
        echo "JHipster Registry failed to start. Check logs with:"
        echo "  $DOCKER_COMPOSE logs jhipster-registry"
        exit 1
    fi
    echo -n "."
    sleep 2
    attempts=$((attempts + 1))
done
echo " READY!"

# Wait for smtp4dev
echo -n "Waiting for smtp4dev..."
attempts=0
max_attempts=30
while ! curl -sf http://localhost:5000/api/server > /dev/null 2>&1; do
    if [ $attempts -ge $max_attempts ]; then
        echo " TIMEOUT!"
        echo ""
        echo "smtp4dev failed to start. Check logs with:"
        echo "  $DOCKER_COMPOSE logs smtp4dev"
        exit 1
    fi
    echo -n "."
    sleep 2
    attempts=$((attempts + 1))
done
echo " READY!"

echo ""
echo "Step 3: Running OAuth2 authentication test..."
echo "----------------------------------------------"

# Check if Python 3 and requests are available
if ! command -v python3 &> /dev/null; then
    echo "Error: Python 3 is required to run the test script"
    echo "Please install Python 3"
    exit 1
fi

# Try to run the test
if python3 -c "import requests" 2>/dev/null; then
    python3 test_oauth2.py
else
    echo "Error: Python 'requests' library not found"
    echo "Install it with: pip3 install requests"
    echo ""
    echo "Or run the test manually after installing:"
    echo "  python3 test_oauth2.py"
    exit 1
fi

echo ""
echo "========================================================================"
echo "Services are running:"
echo "  - smtp4dev Web UI:      http://localhost:5000"
echo "  - JHipster Registry:    http://localhost:8761 (admin/admin)"
echo ""
echo "To stop the services:"
echo "  $DOCKER_COMPOSE down"
echo ""
echo "To view logs:"
echo "  $DOCKER_COMPOSE logs -f"
echo "========================================================================"
