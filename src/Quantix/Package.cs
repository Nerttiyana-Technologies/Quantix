// Quantix — the install package's own assembly.
//
// This assembly is intentionally empty in v1. The Quantix source generator emits fully
// self-contained dispatch code directly into the consumer's assembly, so there is no shared
// runtime helper to ship here (design section 5; plan L3-6). This file exists so the project
// compiles to a valid assembly; any future shared helper would live in this project.
