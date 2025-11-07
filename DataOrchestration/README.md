# Data & Analysis Orchestration Layer

This layer aggregates, normalizes, and prepares telemetry data for AI analysis.

## Components:
- **TelemetryAggregator**: Buffers and timestamps browser events
- **SessionManager**: Tracks debug sessions and snapshots
- **ContextBuilder**: Creates structured JSON for AI prompts
- **DataNormalizer**: Normalizes telemetry to schema
- **RedactionService**: Removes sensitive data

## Responsibilities:
- Aggregate telemetry from browser layer
- Normalize data to schema: {console, network, perf, dom}
- Manage debug sessions
- Build AI analysis context
- Handle data redaction
