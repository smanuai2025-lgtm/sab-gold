#!/bin/bash
# ═══════════════════════════════════════════════════════════════════
# SabeekaGold - Deployment Script
# ═══════════════════════════════════════════════════════════════════

set -e

echo "═══════════════════════════════════════════════════════════════════"
echo "🚀 SabeekaGold - Production Deployment"
echo "═══════════════════════════════════════════════════════════════════"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if .env exists
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found!${NC}"
    echo "Copy .env.example to .env and configure it first."
    exit 1
fi

# Load environment variables
source .env

echo -e "${YELLOW}📦 Step 1: Building Docker images...${NC}"
docker-compose build --no-cache

echo -e "${YELLOW}🛑 Step 2: Stopping existing containers...${NC}"
docker-compose down --remove-orphans

echo -e "${YELLOW}🗃️ Step 3: Starting database and cache...${NC}"
docker-compose up -d sqlserver redis
sleep 30  # Wait for DB to be ready

echo -e "${YELLOW}⚙️ Step 4: Running database migrations...${NC}"
docker-compose run --rm backend dotnet ef database update || true

echo -e "${YELLOW}🚀 Step 5: Starting all services...${NC}"
docker-compose up -d

echo -e "${YELLOW}🔍 Step 6: Health check...${NC}"
sleep 10

# Check backend
if curl -sf http://localhost:5000/health > /dev/null; then
    echo -e "${GREEN}✅ Backend is healthy${NC}"
else
    echo -e "${RED}❌ Backend health check failed${NC}"
fi

# Check frontend
if curl -sf http://localhost/health > /dev/null; then
    echo -e "${GREEN}✅ Frontend is healthy${NC}"
else
    echo -e "${RED}❌ Frontend health check failed${NC}"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════════"
echo -e "${GREEN}✅ Deployment Complete!${NC}"
echo "═══════════════════════════════════════════════════════════════════"
echo ""
echo "🌐 Frontend: https://${DOMAIN}"
echo "🔌 API: https://${DOMAIN}/api"
echo "📊 Hangfire Dashboard: https://${DOMAIN}/api/hangfire"
echo ""
echo "📝 Useful commands:"
echo "   docker-compose logs -f        # View logs"
echo "   docker-compose ps             # Check status"
echo "   docker-compose restart        # Restart services"
echo "   docker-compose down           # Stop all"
echo ""
