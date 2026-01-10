1. System Overview

The User Interface (UI) system in OutlandHaven is designed to be decoupled, event-driven, and data-oriented. It avoids the common pitfall of the UI checking for updates every frame. Instead, it relies on an Observer Pattern where the UI reacts only when data changes, and a Payload Pattern to handle dynamic content (like chests or vendors) using a single, reusable code module.