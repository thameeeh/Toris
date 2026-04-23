Identifier: OutlandHaven.Core.SystemBootstrapper : MonoBehaviour

Architectural Role: Singleton Manager / Application Entry Point

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None.
- Public API: None.

Dependency Graph (Crucial for Scaling):
- Upstream: Requires global ScriptableObject manager instances passed via Inspector array.
- Downstream: Bootstraps the application environment. Assumed to be present in the initial/persistent scene.

Data Schema:
- ScriptableObject[] _persistentManagers -> Array to hold global manager ScriptableObjects, forcing them into memory upon initialization.

Side Effects & Lifecycle:
- Lifecycle likely driven by standard Unity events (`Awake`, `Start`) to instantiate/load required subsystems early in application flow.
- Purpose is to instantiate and enforce persistence of defined ScriptableObjects to ensure global availability and to allow them to register their event listeners immediately.