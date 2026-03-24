![MyCodingAgent by Willem-Jan Beltman - Logo]mycodingagentbywillembeltman.png

# 🚀 MyCodingAgent – .NET-first AI Development Engine

MyCodingAgent is an **Ollama-first, .NET-focused development system** that turns LLMs into reliable engineering agents instead of unpredictable code generators.

It acts as a structured layer between your codebase and local LLMs, enforcing correctness, consistency, and architecture through **Roslyn-powered validation and controlled generation pipelines**.

# 🔑 Core Principles
## 🧠 Ollama First

Runs entirely on **local models via Ollama**.
No cloud dependency. No hidden costs. Full control over models, context, and behavior.

## ⚙️ .NET Only (on purpose)

MyCodingAgent is built exclusively for the .NET ecosystem:

- Blazor (frontend)
- Web API
- Console apps & workers
- Service Bus / microservices
- Docker-based environments
- Infrastructure as Code

This focus allows **deep integration instead of shallow generalization**.

## 🛡️ Roslyn as Gatekeeper

All generated code is validated through the Roslyn compiler pipeline:

- Prevents invalid code from entering the system
- Enables semantic analysis instead of string-based guessing
- Forces LLM output into compilable, structured reality

# 🧩 What It Actually Does

MyCodingAgent is not “AI that writes code”.

It is:

- A **controlled code generation system**
- A **context-aware prompt engine**
- A **deterministic orchestration layer for LLM workflows**

Instead of letting an LLM “figure things out”, MyCodingAgent:

1. Defines the task
2. Limits the context
3. Generates targeted output
4. Validates via Roslyn
5. Iterates until correct

# 🔭 Roadmap

## Deterministic Event Sourcing

Rebuild system state from a minimal starting point using:

- Event sourcing
- Fully reproducible generation steps
- No hidden mutations

## Integrated Project Management
- Built-in task/subtask system
- Local version control integration
- Prompt generation based on actual project state

## RAG + Analyzer Tooling
- Retrieval-Augmented Generation tailored for .NET
- Roslyn analyzers feeding structured insights into LLM context
- Smarter, safer code generation

## MCP Architecture (Future)
- Distributed agent system
- Modular capability providers
- But first: a strong standalone core

# 💡 Why This Exists

LLMs are powerful, but unreliable by default.

MyCodingAgent exists to:

- Remove randomness
- Enforce structure
- Make LLMs usable for real engineering work

But also because there are no tools like this that use Ollama that I know off.
And I could use the experience.

# ⚠️ Status

Work in progress.
Built for experimentation, iteration, and pushing the limits of local AI-driven development.
I've booked very good results with testing but it's not ready jet. 
Most likely there are errors and deadlocks as I'm still building the foundation right.