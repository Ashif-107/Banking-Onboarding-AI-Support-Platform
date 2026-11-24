# IncedoBank — Root README

<div align="center">
  <h1>🏦 IncedoBank Digital Suite</h1>
  <p>Full-stack banking onboarding platform + AI Agent Cockpit</p>
  <p>
    <strong>Next.js 16 · .NET 8 · Groq AI · HuggingFace · Neon · Cloudflare R2</strong>
  </p>
  <p>
    <img src="https://img.shields.io/badge/cost-$0%2Fmonth-brightgreen" />
    <img src="https://img.shields.io/badge/stack-full--stack-blue" />
    <img src="https://img.shields.io/badge/AI-Groq%20Llama%203.3-orange" />
  </p>
</div>

---

## 🚀 Quick Start (Local Dev)

### Prerequisites
- Node.js 20+ and pnpm
- .NET 8 SDK
- Docker Desktop (for local PostgreSQL + Redis)

### 1. Start local services
```bash
docker-compose up -d
# Starts: PostgreSQL (5432), Redis (6379), Meilisearch (7700)
```

### 2. Frontend
```bash
cd frontend
cp .env.local.example .env.local
# Fill in Clerk keys from https://clerk.dev (free)
pnpm install
pnpm dev
# → http://localhost:3000
```

### 3. Backend
```bash
cd backend/IncedoBank.API
cp appsettings.json appsettings.Development.json
# Fill in API keys (Groq, HuggingFace, R2)
# Then run:
dotnet restore
dotnet run
# → http://localhost:5000
# → API Docs: http://localhost:5000/api-docs
```

---

## 🌐 Free Service Setup Guides

### Clerk.dev (Auth)
1. Go to https://clerk.dev → Create free account
2. Create app → Copy Publishable Key + Secret Key → paste in `frontend/.env.local`
3. Copy JWKS URL → paste in `backend/appsettings.json` under `Clerk`

### Groq API (AI)
1. Go to https://console.groq.com → Create free account
2. API Keys → Create key → paste in `backend/appsettings.json` under `Groq:ApiKey`

### HuggingFace (Sentiment)
1. Go to https://huggingface.co → Create free account
2. Settings → Access Tokens → Create token → paste in `backend/appsettings.json`

### Neon PostgreSQL (Production DB)
1. Go to https://neon.tech → Create free project
2. Copy connection string → set as `ConnectionStrings:DefaultConnection` on Render

### Cloudflare R2 (Document Storage)
1. Go to https://dash.cloudflare.com → R2 → Create bucket `incedo-bank-docs`
2. Manage R2 API Tokens → Create token → Copy AccessKeyId + SecretAccessKey

### Resend (Email)
1. Go to https://resend.com → Create free account
2. API Keys → Create key → paste in `backend/appsettings.json`

---

## 📂 Project Structure

```
incedo-bank/
├── frontend/                    # Next.js 16 (App Router)
│   └── src/
│       ├── app/
│       │   ├── (auth)/          # Sign-in, Sign-up (Clerk)
│       │   ├── (customer)/      # Dashboard, Apply, KYC, Notifications
│       │   └── (agent)/         # AI Agent Cockpit
│       ├── components/ui/       # shadcn/ui components
│       └── lib/
│           ├── api/             # Axios client
│           ├── ocr/             # Tesseract.js OCR
│           └── websocket/       # SignalR hook
│
├── backend/
│   └── IncedoBank.API/          # .NET 8 ASP.NET Core
│       ├── Controllers/         # REST API controllers
│       ├── Domain/              # Entity models
│       ├── Infrastructure/      # EF Core, DbContext
│       ├── Services/
│       │   ├── AI/              # Groq + HuggingFace services
│       │   ├── Storage/         # Cloudflare R2 service
│       │   └── NBA/             # Next-Best Action engine
│       └── Hubs/                # SignalR notification hub
│
├── docker-compose.yml           # Local dev services
├── render.yaml                  # Render.com deployment
└── .github/workflows/           # GitHub Actions CI/CD
```

---

## 🤖 AI Features

| Feature | Model | Provider | Cost |
|---|---|---|---|
| Ticket Summarization | Llama 3.3 70B | Groq | Free |
| Response Generation | Llama 3.3 70B | Groq | Free |
| Eligibility Pre-Screening | Llama 3.3 70B | Groq | Free |
| Sentiment Analysis | RoBERTa | HuggingFace | Free |
| Emotion Detection | DistilRoBERTa | HuggingFace | Free |
| Document OCR | Tesseract.js | Browser | Free |
| Face Detection | face-api.js | Browser | Free |

---

## 💰 Total Monthly Cost: $0

All services run on permanent free tiers. No credit card required for development.
