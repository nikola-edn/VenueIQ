# 1. Introduction

This document defines the complete architecture for **VenueIQ**, a bilingual (**Serbian Latin / English**) .NET MAUI (mobile + desktop) application that helps users identify optimal business locations in Serbia using **Azure Maps**. It aligns with the PRD (prd.md) and is optimized for rapid MVP delivery with professional extensibility.

**Relationship to Frontend Architecture:** VenueIQ is a client-only app (no custom backend in MVP). The MAUI app uses Azure Maps Web control (via WebView) and REST APIs for POI search. Future server components can be added without breaking the client boundary. Refer to `docs/ui-architecture.md` for detailed UI architecture guidance.

> Environment Constraint: WSL + MAUI
>
> This repository is currently edited within WSL. Although `dotnet` is installed, .NET MAUI projects cannot be built, run, or UI-tested in this environment. Contributors and agents should limit actions to writing code and documentation; do not attempt builds/tests locally here. The maintainer will perform builds and manual testing on a proper MAUI host (Windows/macOS with the MAUI workloads installed).

---
