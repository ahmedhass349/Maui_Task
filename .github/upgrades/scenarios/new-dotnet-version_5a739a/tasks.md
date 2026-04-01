# Maui_Task .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the Maui_Task solution upgrade from .NET 9 to .NET 10.0. All four projects will be upgraded simultaneously in a single atomic operation, followed by verification.

**Progress**: 0/3 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [ ] TASK-001: Verify prerequisites
**References**: Plan §2 Phase 0

- [ ] (1) Verify .NET 10.0 SDK is installed and available
- [ ] (2) SDK version meets .NET 10.0 requirements (**Verify**)

---

### [ ] TASK-002: Atomic framework and dependency upgrade with compilation fixes
**References**: Plan §4 Step 1, Plan §5 Package Update Reference, Plan §7 Breaking Changes Catalog, Plan §6 Project-by-Project Specifications

- [ ] (1) Update TargetFramework properties in all 4 projects per Plan §6: Maui_Task.Shared to net10.0, Maui_Task.Web.Client to net10.0, Maui_Task.Web to net10.0, Maui_Task to net10.0-android/ios/maccatalyst/windows10.0.19041.0
- [ ] (2) All project TargetFramework properties updated (**Verify**)
- [ ] (3) Update all 12 package references across projects to version 10.0.5 per Plan §5 Package Update Reference (EntityFrameworkCore, AspNetCore components, SignalR, WebAssembly, Authentication, Mvc, Logging packages)
- [ ] (4) All package references updated to target versions (**Verify**)
- [ ] (5) Restore all dependencies using dotnet restore
- [ ] (6) All dependencies restored successfully (**Verify**)
- [ ] (7) Build entire solution and fix all compilation errors per Plan §7 Breaking Changes Catalog (focus areas: Authentication/IdentityModel migration to Microsoft.IdentityModel, MAUI Controls BindingMode API changes, MauiAppBuilder.Services incompatibilities, SecureStorage method signature updates)
- [ ] (8) Solution builds with 0 errors (**Verify**)

---

### [ ] TASK-003: Final commit
**References**: Plan §10 Source Control Strategy

- [ ] (1) Commit all changes with message: "TASK-003: Complete atomic .NET 10.0 upgrade of all projects"

---