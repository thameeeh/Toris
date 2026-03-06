## 2024-05-18 - Unity sqrMagnitude vs Vector3.Distance
**Learning:** `Vector3.Distance` calls `Mathf.Sqrt` which is computationally expensive, especially in `Update()` loops and for frequent spawns.
**Action:** Replace `Vector3.Distance(a, b) < dist` with `(a - b).sqrMagnitude < dist * dist` for performance gains.
