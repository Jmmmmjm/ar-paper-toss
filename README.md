# 🗑️ AR Paper Toss

[![Unity 6](https://img.shields.io/badge/Unity-6000.1.6f1-black?logo=unity)](https://unity.com/)
[![AR Foundation](https://img.shields.io/badge/AR%20Foundation-6.1.1-blue)](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/)
[![Apple ARKit](https://img.shields.io/badge/Apple-ARKit%206.1.1-silver?logo=apple)](https://developer.apple.com/augmented-reality/arkit/)
[![CI/CD](https://img.shields.io/badge/GitHub%20Actions-iOS%20IPA%20Build-2088FF?logo=github-actions)](.github/workflows/build-ios.yml)
[![Render Pipeline](https://img.shields.io/badge/URP-17.1.0-orange)](https://unity.com/srp/universal-render-pipeline)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A retro 8-bit augmented reality arcade experience built in **Unity 6** with **AR Foundation** and **Apple ARKit**. 

Flick crumpled paper balls into a 3D trash can placed directly in your real-world environment. Navigate shifting crosswinds, evade patrolling paper airplane obstacles, trigger escalating combo fireworks, and rack up high scores with authentic chiptune audio and tactile haptic feedback.

---

## 🌟 Key Features

### 📍 Augmented Reality & Physical Surface Tracking
- **Smart AR Tap-to-Place:** Uses `ARRaycastManager` to scan your physical environment and detect horizontal planes (floors, desks, tables), with an Editor simulation fallback for instant PC testing.
- **Real-Room Physics Collisions:** Detected AR planes (walls, floors, furniture) carry active invisible physics colliders (`ARPlaneCollisionHandler`), allowing errant paper ball tosses to bounce realistically off your actual room.
- **Juicy Spawn-In Animation:** Procedural drop bounce animation with multi-stage elastic squash-and-stretch and particle dust poofs when placing the trash can (`TrashCanAnimator`).

### 🏀 Aerodynamic Toss Physics & Flick Mechanics
- **Intuitive Touch Controls:** Smooth flick/swipe velocity calculation with configurable power sensitivity (*Low*, *Medium*, *High*).
- **Realistic Paper Aerodynamics:** Quadratic air resistance, micro-turbulence flight flutter (Perlin noise air wobble), launch spin torque, and anti-tunneling continuous dynamic sphere-casting.
- **Juicy Rim Reactions:** The trash can springs, tilts, and wobbles when struck along its rim or outer walls.

### 💨 Dynamic Crosswinds
- Dynamic wind system (`WindManager`) that shifts speed (0 to 8 MPH) and direction periodically.
- World-space crosswind physics directly alter paper ball trajectories mid-air, challenging player precision.

### ✈️ Acrobatic Paper Airplane Obstacle
- Fully 3D folded paper airplane obstacle hovering and patrolling over the trash can rim (`PaperAirplaneController`).
- Features dynamic aerodynamic banking into turns, acrobatic loop swoops, and hoop hover blocking.
- Inelastic deflection physics and an acrobatic spin-out recovery routine when hit by a paper ball, complete with dedicated deflection SFX and sparks.

### 🎆 Retro 8-Bit Arcade Polish & Juice
- **Pocket GUI Retro UI:** Styled retro arcade UI featuring the `PressStart2P` pixel font, animated floating combo popups (`* SWISH! *`, `* COMBO x3! *`, `* NEW RECORD! *`), and safe-area notch layout.
- **Tiered Celebration VFX:** Particle celebrations via Cartoon FX Remaster scale with your combo streak:
  - **Tier 1 (1x Swish):** Clean light burst & gentle falling stars.
  - **Tier 2 (2x Double):** Cyan-purple fireworks explosion & star shower.
  - **Tier 3 (3x On Fire!):** Intense flame burst & golden sparks.
  - **Tier 4 (4x Lightning!):** Electric plasma arcs & high-voltage sparks.
  - **Tier 5+ (5x+ Godlike):** Mega rainbow fireworks and celebratory golden rays.
- **Dynamic 8-Bit Chiptune Audio:** Self-healing `AudioSource` pool with pitch-escalating combo jingles, thuds, misses, UI clicks, and catchy retro background music.
- **Haptic Vibration Feedback:** Immediate physical feedback on ball launch, basket scores, and button presses.

---

## 🏗️ Architecture & Project Structure

The project follows a clean, decoupled event-driven architecture:

```
AR Paper Toss/
├── Assets/
│   ├── Editor/
│   │   ├── iOSBuildScript.cs         # Automated headless build & XR ARKit config
│   │   └── ARPaperToss.Editor.asmdef # Assembly definition for CI/CD batchmode
│   ├── Prefabs/
│   │   ├── ARPlane.prefab            # Invisible AR plane collider surface
│   │   ├── PaperAirplane.prefab      # 3D folded airplane obstacle with trail
│   │   ├── PaperBall.prefab          # Physics-driven paper projectile
│   │   ├── PlacementIndicator.prefab # Holographic surface placement ring
│   │   ├── RealisticRoom.prefab      # XR simulation room for Editor testing
│   │   └── TrashCan.prefab           # Animated trash can with inner score trigger
│   ├── Scripts/
│   │   ├── AR/
│   │   │   ├── ARPlaneCollisionHandler.cs # Physics colliders on detected AR planes
│   │   │   ├── ARSessionManager.cs        # AR lifecycle & tracking validation
│   │   │   ├── PlacementController.cs     # Raycast placement & reticle logic
│   │   │   └── PlacementIndicator.cs      # Smooth visual indicator tracking
│   │   ├── Audio/
│   │   │   ├── AudioManager.cs            # Pooled 8-bit SFX & BGM controller
│   │   │   └── HapticManager.cs           # iOS/Android device vibration feedback
│   │   ├── Gameplay/
│   │   │   ├── ObstacleManager.cs         # Spawns & coordinates airplane obstacle
│   │   │   ├── PaperAirplaneController.cs # Flight patterns, banking & tumble recovery
│   │   │   ├── PaperBall.cs               # Aerodynamic drag, flutter & collision
│   │   │   ├── PaperBallLauncher.cs       # Touch flick gestures & ball spawning
│   │   │   ├── ScoreTrigger.cs            # Inner basket validation & confetti trigger
│   │   │   ├── TrashCanAnimator.cs        # Procedural spawn drop & rim recoil wobble
│   │   │   ├── VFXManager.cs              # Multi-tier streak celebration particles
│   │   │   └── WindManager.cs             # Crosswind vector generation & events
│   │   └── UI/
│   │       ├── MainMenuUI.cs              # Retro title screen, tutorials, navigation
│   │       ├── SafeArea.cs                # Device notch & home bar dynamic anchoring
│   │       ├── ScoreboardUI.cs            # Zero-padded HUD, combo banners, refresh
│   │       ├── SettingsManager.cs         # Audio, haptic, sensitivity persistence
│   │       └── SettingsMenuUI.cs          # Modal settings drawer with reset confirmation
│   └── Scenes/
│       └── SampleScene.unity              # Master AR gameplay scene
├── .github/
│   └── workflows/
│       └── build-ios.yml                  # Automated macOS GitHub Actions CI/CD
└── IOS_BUILD_GUIDE.md                     # Complete blueprint for iOS cloud builds
```

---

## 🚀 CI/CD & Automated Cloud Builds

Building iOS AR apps without a dedicated macOS machine is supported out of the box via GitHub Actions.

```mermaid
flowchart LR
    A[Git Push / Workflow Dispatch] --> B[GitHub Actions macos-14]
    B --> C[Select Xcode 16]
    C --> D[Headless Unity 6 Activation]
    D --> E[Batchmode iOS Export & ARKit Registration]
    E --> F[xcodebuild Archive]
    F --> G[Unsigned .ipa Package]
    G --> H[Upload Artifact]
    H --> I[Sideload via Sideloadly / AltStore]
```

- **Workflow:** [`.github/workflows/build-ios.yml`](.github/workflows/build-ios.yml)
- **Runner:** `macos-14` (Apple Silicon M1/M2)
- **Toolchain:** Xcode 16 + Unity 6 (`6000.1.6f1`) with iOS Build Support
- **Output:** An unsigned `.ipa` artifact ready for free 7-day personal Apple ID sideloading (no $99/yr developer account required).
- **Technical Retrospective:** See [`IOS_BUILD_GUIDE.md`](IOS_BUILD_GUIDE.md) for root-cause solutions regarding headless Unity licensing, ARKit native symbol registration, and Swift 6 shims.

---

## 🎮 Getting Started & Development

### Prerequisites
- **Unity:** Version `6000.1.6f1` (Unity 6)
- **Modules:** iOS Build Support (if building locally for iOS)
- **Hardware:**
  - In-Editor: Any Windows/macOS PC with Unity (uses XR Simulation environment)
  - On-Device: iPhone / iPad running iOS 15.0+ with ARKit support

### Running in the Unity Editor
1. Clone this repository:
   ```bash
   git clone https://github.com/Jmmmmjm/ar-paper-toss.git
   ```
2. Open the project in **Unity Hub** using version `6000.1.6f1`.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.
5. Use your mouse to aim at the simulated floor/desk surface in the test room, click to place the trash can, and flick the paper ball with mouse swipes.

### Triggering a Cloud Build
1. Push your changes to GitHub or navigate to the **Actions** tab in your repository.
2. Ensure repository secrets `UNITY_EMAIL` and `UNITY_PASSWORD` are configured.
3. Run the **Build iOS IPA** workflow manually via `workflow_dispatch`.
4. Once completed, download the `ARPaperToss-iOS-IPA` artifact and sideload using [Sideloadly](https://sideloadly.io/) or [AltStore](https://altstore.io/).

---

## 🕹️ Controls & How to Play

| Action | Control (Device) | Control (Editor) |
| :--- | :--- | :--- |
| **Place Trash Can** | Move camera over floor/table, tap green ring | Aim reticle with mouse, Left Click |
| **Toss Paper Ball** | Swipe / flick up from bottom of screen | Click and drag upward, release |
| **Adjust Aim** | Swipe at an angle to curve ball left/right | Drag diagonally |
| **Reposition Can** | Tap `[REFRESH]` button on the top HUD | Click `[REFRESH]` button |
| **Change Settings** | Tap `[SETTINGS]` to adjust sound or flick power | Click `[SETTINGS]` button |

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
