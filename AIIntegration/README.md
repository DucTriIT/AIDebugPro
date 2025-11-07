# AI Engine Integration Layer

This layer handles all AI/LLM interactions and response processing.

## Components:
- **Interfaces**: IAIClient abstraction
- **Clients**:
  - OpenAIClient (GPT-4/5 API)
  - LocalLLMClient (Ollama, LM Studio, Mistral)
- **PromptComposer**: Builds AI prompts with telemetry
- **ResponseParser**: Parses AI output to structured results
- **TokenManager**: Optional caching and token limiting

## Responsibilities:
- Abstract AI provider differences
- Compose prompts with instructions + telemetry JSON
- Parse AI responses into structured format
- Manage API calls and rate limiting
- Handle both cloud and local LLMs
