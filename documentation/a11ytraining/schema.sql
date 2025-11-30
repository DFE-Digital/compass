-- Accessibility Training Database Schema

-- Create schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS accessibility_manual;

-- Training Sessions Table
-- Stores unique training session codes for intermediate training
CREATE TABLE IF NOT EXISTS accessibility_manual.training_sessions (
    id SERIAL PRIMARY KEY,
    unique_code VARCHAR(9) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Answers Table
-- Stores user answers for intermediate training questions
CREATE TABLE IF NOT EXISTS accessibility_manual.answers (
    training_session_id INTEGER NOT NULL REFERENCES accessibility_manual.training_sessions(id) ON DELETE CASCADE,
    question_number INTEGER NOT NULL,
    question_type VARCHAR(50) NOT NULL, -- 'multipleChoice', 'trueFalse', 'multipleSelect'
    user_answer TEXT, -- Comma-separated indices for multiple select, single index for others
    answer_status VARCHAR(20) NOT NULL, -- 'Correct', 'Incorrect', 'Not answered'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (training_session_id, question_number)
);

-- Sent Codes Table
-- Tracks when training codes are emailed to users
CREATE TABLE IF NOT EXISTS accessibility_manual.sent_codes (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    code VARCHAR(9) NOT NULL,
    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_training_sessions_code ON accessibility_manual.training_sessions(unique_code);
CREATE INDEX IF NOT EXISTS idx_answers_session ON accessibility_manual.answers(training_session_id);
CREATE INDEX IF NOT EXISTS idx_answers_question ON accessibility_manual.answers(question_number);
CREATE INDEX IF NOT EXISTS idx_sent_codes_email ON accessibility_manual.sent_codes(email);
CREATE INDEX IF NOT EXISTS idx_sent_codes_code ON accessibility_manual.sent_codes(code);

