-- ============================================================
-- IncedoBank — PostgreSQL Init Script
-- Run automatically on first docker-compose up
-- ============================================================

-- Enable extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "vector";          -- pgvector for RAG
CREATE EXTENSION IF NOT EXISTS "pg_trgm";         -- trigram search

-- ============================================================
-- CUSTOMERS
-- ============================================================
CREATE TABLE IF NOT EXISTS customers (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    clerk_user_id   VARCHAR(255) UNIQUE NOT NULL,
    email           VARCHAR(255) UNIQUE NOT NULL,
    phone           VARCHAR(20),
    security_level  SMALLINT NOT NULL DEFAULT 1,   -- 1=basic, 2=kyc_done, 3=full
    status          VARCHAR(50) NOT NULL DEFAULT 'pending_kyc',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PROFILES
-- ============================================================
CREATE TABLE IF NOT EXISTS profiles (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id         UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    full_name           VARCHAR(255),
    dob                 DATE,
    pan_number          VARCHAR(10),
    aadhaar_hash        VARCHAR(64),               -- SHA-256 only, never plaintext
    gender              VARCHAR(10),
    address             JSONB,                     -- { line1, city, state, pincode }
    occupation          VARCHAR(100),
    annual_income       BIGINT,
    ocr_extracted_data  JSONB,                     -- Raw Tesseract.js output
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(customer_id)
);

-- ============================================================
-- KYC RECORDS
-- ============================================================
CREATE TABLE IF NOT EXISTS kyc_records (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id     UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    status          VARCHAR(50) NOT NULL DEFAULT 'pending',  -- pending|in_review|approved|rejected
    verified_by     VARCHAR(255),
    verified_at     TIMESTAMPTZ,
    rejection_reason TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(customer_id)
);

-- ============================================================
-- KYC DOCUMENTS
-- ============================================================
CREATE TABLE IF NOT EXISTS kyc_documents (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id     UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    kyc_record_id   UUID NOT NULL REFERENCES kyc_records(id) ON DELETE CASCADE,
    doc_type        VARCHAR(50) NOT NULL,          -- aadhaar|pan|passport|payslip|selfie
    r2_object_key   VARCHAR(500) NOT NULL,         -- Cloudflare R2 key
    file_size_bytes BIGINT,
    mime_type       VARCHAR(100),
    ocr_text        TEXT,                          -- Tesseract.js output
    ocr_confidence  DECIMAL(5,2),
    face_detected   BOOLEAN,                       -- face-api.js result
    status          VARCHAR(50) NOT NULL DEFAULT 'uploaded',
    version         SMALLINT NOT NULL DEFAULT 1,
    uploaded_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- PRODUCTS (dynamic product catalog)
-- ============================================================
CREATE TABLE IF NOT EXISTS products (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code            VARCHAR(50) UNIQUE NOT NULL,   -- savings_account|credit_card|home_loan
    name            VARCHAR(255) NOT NULL,
    description     TEXT,
    form_schema     JSONB NOT NULL,                -- JSON Schema for the application form
    eligibility_rules JSONB,                       -- Rules for pre-screening
    sla_hours       INTEGER NOT NULL DEFAULT 72,   -- SLA in hours
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- APPLICATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS applications (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id             UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    product_id              UUID NOT NULL REFERENCES products(id),
    status                  VARCHAR(50) NOT NULL DEFAULT 'draft', -- draft|submitted|in_review|approved|rejected
    form_data               JSONB NOT NULL DEFAULT '{}',
    ai_eligibility_score    SMALLINT,              -- 0-100, Groq output
    ai_eligibility_reason   TEXT,
    sla_deadline            TIMESTAMPTZ,
    current_stage           VARCHAR(100),
    assigned_to             VARCHAR(255),          -- agent clerk_user_id
    rejection_reason        TEXT,
    approved_at             TIMESTAMPTZ,
    rejected_at             TIMESTAMPTZ,
    submitted_at            TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- APPLICATION WORKFLOW STAGES
-- ============================================================
CREATE TABLE IF NOT EXISTS workflow_stages (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    application_id  UUID NOT NULL REFERENCES applications(id) ON DELETE CASCADE,
    stage_name      VARCHAR(100) NOT NULL,
    stage_order     SMALLINT NOT NULL,
    status          VARCHAR(50) NOT NULL DEFAULT 'pending', -- pending|in_progress|completed|skipped
    assignee_id     VARCHAR(255),
    notes           TEXT,
    started_at      TIMESTAMPTZ,
    completed_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- TICKETS (Support)
-- ============================================================
CREATE TABLE IF NOT EXISTS tickets (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id         UUID REFERENCES customers(id) ON DELETE SET NULL,
    assigned_agent_id   VARCHAR(255),
    channel             VARCHAR(50) NOT NULL DEFAULT 'web', -- web|email|chat|phone
    subject             VARCHAR(500),
    raw_text            TEXT NOT NULL,
    ai_summary          TEXT,                      -- Groq summarization output
    tags                TEXT[],                    -- Groq auto-tags
    language            VARCHAR(10) DEFAULT 'en',
    sentiment_label     VARCHAR(20),               -- positive|negative|neutral
    sentiment_score     DECIMAL(4,3),              -- 0.000 to 1.000
    emotion_breakdown   JSONB,                     -- { anger, joy, fear, sadness, surprise }
    churn_risk          DECIMAL(4,3),              -- 0.000 to 1.000
    complexity_score    SMALLINT,                  -- 1-10, Groq output
    status              VARCHAR(50) NOT NULL DEFAULT 'open', -- open|in_progress|resolved|closed
    resolved_at         TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- TICKET RESPONSES (AI suggested + agent edits)
-- ============================================================
CREATE TABLE IF NOT EXISTS ticket_responses (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id       UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    agent_id        VARCHAR(255) NOT NULL,
    ai_draft        TEXT,                          -- Groq draft (before agent edit)
    final_response  TEXT NOT NULL,
    tone_used       VARCHAR(50),                   -- formal|empathetic|concise
    was_edited      BOOLEAN NOT NULL DEFAULT FALSE,
    sent_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NBA ACTIONS LOG
-- ============================================================
CREATE TABLE IF NOT EXISTS nba_action_logs (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id       UUID REFERENCES tickets(id),
    customer_id     UUID REFERENCES customers(id),
    agent_id        VARCHAR(255) NOT NULL,
    action_type     VARCHAR(100) NOT NULL,         -- upgrade_offer|waive_fee|schedule_call|escalate
    action_data     JSONB,
    prediction_score DECIMAL(4,3),                 -- AI confidence
    outcome         VARCHAR(50),                   -- accepted|rejected|pending
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- POLICY CHUNKS (RAG — policy docs for agent responses)
-- ============================================================
CREATE TABLE IF NOT EXISTS policy_chunks (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_name   VARCHAR(255) NOT NULL,
    section         VARCHAR(255),
    chunk_text      TEXT NOT NULL,
    embedding       VECTOR(384),                   -- all-MiniLM-L6-v2 dimensions
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS policy_chunks_embedding_idx 
    ON policy_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- ============================================================
-- AUDIT EVENTS (immutable)
-- ============================================================
CREATE TABLE IF NOT EXISTS audit_events (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type     VARCHAR(50) NOT NULL,          -- customer|application|ticket|kyc_document
    entity_id       UUID NOT NULL,
    actor_id        VARCHAR(255) NOT NULL,          -- clerk_user_id
    actor_role      VARCHAR(50),                   -- customer|agent|admin|system
    action          VARCHAR(100) NOT NULL,
    old_values      JSONB,
    new_values      JSONB,
    ip_address      VARCHAR(45),
    user_agent      TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
-- Audit is append-only — no UPDATE or DELETE via app layer, enforce via RLS

-- ============================================================
-- NOTIFICATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS notifications (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id     UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    type            VARCHAR(100) NOT NULL,         -- application_approved|kyc_rejected|ticket_resolved
    title           VARCHAR(500) NOT NULL,
    body            TEXT NOT NULL,
    is_read         BOOLEAN NOT NULL DEFAULT FALSE,
    metadata        JSONB,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- SEED — Default Products
-- ============================================================
INSERT INTO products (code, name, description, form_schema, eligibility_rules, sla_hours) VALUES
(
    'savings_account',
    'Savings Account',
    'A basic savings account with competitive interest rates.',
    '{
        "fields": [
            {"name": "account_type", "type": "select", "label": "Account Type", "options": ["Individual", "Joint"], "required": true},
            {"name": "initial_deposit", "type": "number", "label": "Initial Deposit (₹)", "min": 500, "required": true},
            {"name": "nominee_name", "type": "text", "label": "Nominee Name", "required": false}
        ]
    }',
    '{"min_age": 18, "kyc_required": true}',
    24
),
(
    'credit_card',
    'Credit Card',
    'Lifetime-free credit card with 2% cashback on all spends.',
    '{
        "fields": [
            {"name": "card_variant", "type": "select", "label": "Card Variant", "options": ["Classic", "Gold", "Platinum"], "required": true},
            {"name": "credit_limit_requested", "type": "number", "label": "Requested Credit Limit (₹)", "min": 10000, "required": true},
            {"name": "employer_name", "type": "text", "label": "Employer Name", "required": true},
            {"name": "monthly_income", "type": "number", "label": "Monthly Income (₹)", "required": true}
        ]
    }',
    '{"min_age": 21, "min_income": 25000, "kyc_required": true}',
    72
),
(
    'personal_loan',
    'Personal Loan',
    'Instant personal loan up to ₹10 lakhs at 10.5% p.a.',
    '{
        "fields": [
            {"name": "loan_amount", "type": "number", "label": "Loan Amount (₹)", "min": 10000, "max": 1000000, "required": true},
            {"name": "tenure_months", "type": "select", "label": "Tenure", "options": [12, 24, 36, 48, 60], "required": true},
            {"name": "purpose", "type": "select", "label": "Loan Purpose", "options": ["Medical", "Education", "Travel", "Home Renovation", "Other"], "required": true}
        ]
    }',
    '{"min_age": 21, "min_income": 20000, "kyc_required": true}',
    48
),
(
    'fixed_deposit',
    'Fixed Deposit',
    '7.5% p.a. interest on FDs from 7 days to 10 years.',
    '{
        "fields": [
            {"name": "deposit_amount", "type": "number", "label": "Deposit Amount (₹)", "min": 1000, "required": true},
            {"name": "tenure_days", "type": "number", "label": "Tenure (Days)", "min": 7, "max": 3650, "required": true},
            {"name": "payout_frequency", "type": "select", "label": "Interest Payout", "options": ["On Maturity", "Monthly", "Quarterly", "Annually"], "required": true}
        ]
    }',
    '{"min_age": 18, "kyc_required": true}',
    24
),
(
    'home_loan',
    'Home Loan',
    'Up to ₹5 crore home loan at 8.5% p.a. with 30-year tenure.',
    '{
        "fields": [
            {"name": "loan_amount", "type": "number", "label": "Loan Amount (₹)", "min": 500000, "required": true},
            {"name": "property_type", "type": "select", "label": "Property Type", "options": ["Under Construction", "Ready to Move", "Resale"], "required": true},
            {"name": "co_applicant_name", "type": "text", "label": "Co-Applicant Name (optional)", "required": false},
            {"name": "tenure_years", "type": "number", "label": "Tenure (Years)", "min": 1, "max": 30, "required": true}
        ]
    }',
    '{"min_age": 21, "max_age": 65, "min_income": 50000, "kyc_required": true}',
    120
),
(
    'current_account',
    'Current Account',
    'Zero-balance current account for businesses with unlimited transactions.',
    '{
        "fields": [
            {"name": "business_name", "type": "text", "label": "Business Name", "required": true},
            {"name": "business_type", "type": "select", "label": "Business Type", "options": ["Proprietorship", "Partnership", "LLP", "Private Ltd", "Public Ltd"], "required": true},
            {"name": "gstin", "type": "text", "label": "GSTIN", "required": false},
            {"name": "expected_monthly_turnover", "type": "number", "label": "Expected Monthly Turnover (₹)", "required": true}
        ]
    }',
    '{"min_age": 18, "kyc_required": true, "business_docs_required": true}',
    72
)
ON CONFLICT (code) DO NOTHING;
