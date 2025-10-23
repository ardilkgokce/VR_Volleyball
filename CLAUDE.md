# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VR Volleyball is a Unity 2022.3.62f2 VR sports simulation game where a VR player competes against AI-controlled bots in volleyball matches. The game uses OpenXR for VR compatibility (Meta Quest optimized) and features state machine-based AI, real-time ball physics, player registration/ranking systems, and multiple game environments.

## Development Commands

### Unity Editor
```bash
# Open project in Unity
# Use Unity Hub with Unity 2022.3.62f2 LTS

# Build for VR (via Unity Editor)
# File > Build Settings > Select target platform (Android for Quest)
```

### Git Workflow
```bash
# Check status
git status

# View recent commits
git log --oneline -10

# Create feature branch
git checkout -b feature/your-feature-name
```

### Testing
The project uses Unity Test Framework (com.unity.test-framework@1.1.33). Tests can be run via Unity's Test Runner window (Window > General > Test Runner).

## Architecture Overview

### Core Systems

1. **Game Management (GameManager.cs)**
   - Singleton pattern orchestrating the entire match
   - Handles scoring, set/match completion, rally management
   - Manages bot instantiation and positioning
   - Coordinates UI updates and audio feedback
   - Entry point: `Assets/Scripts/Bot/GameManager.cs`

2. **Bot AI System (State Machine Pattern)**
   - **BotController.cs** (1420 lines): Core AI controller with state machine
   - **BotState.cs**: Abstract base class for all states
   - Concrete states: IdleState, ServiceState, PreparingHitState, HittingState, MovingToBallState, ReturningToPositionState
   - Uses physics-based ball trajectory prediction to calculate landing points
   - Enforces 3-hit rule and intelligent target selection

3. **Ball Physics (VolleyballBall.cs)**
   - Tracks hit count and enforces 3-hit rule
   - Detects drop zones (in/out, red/blue court)
   - Awards points via GameManager integration
   - Provides visual/audio feedback on hits

4. **Court Management (VolleyballCourtManager.cs)**
   - Procedural court generation (18m × 9m, 2.43m net height)
   - Renders court lines, net, and boundaries
   - Team side visual differentiation

5. **VR Player Integration**
   - **VRPlayerProxy.cs**: Represents VR player in game world, bridges VR input to game logic
   - **VRHandController.cs**: Handles hand collision, ball hitting with dynamic trajectory calculation based on hand speed (5 speed thresholds from soft sets to hard spikes)

6. **Player Persistence (PlayerDataManager.cs)**
   - CSV-based player data storage in `Application.persistentDataPath`
   - Registration form handling
   - Score tracking and ranking integration

### Directory Structure

```
Assets/
├── Scripts/
│   ├── [Root gameplay scripts]
│   │   ├── VolleyballCourtManager.cs
│   │   ├── VolleyballBall.cs
│   │   ├── VRPlayerProxy.cs
│   │   ├── VRHandController.cs
│   │   └── AnimationController.cs
│   │
│   ├── Bot/                          # AI System
│   │   ├── GameManager.cs            # Game orchestration
│   │   ├── BotController.cs          # AI controller (state machine)
│   │   ├── BotState.cs               # Abstract state base
│   │   ├── IdleState.cs
│   │   ├── ServiceState.cs
│   │   └── [Other state classes]
│   │
│   └── [Support Scripts]
│       ├── PlayerDataManager.cs      # Player persistence
│       ├── RankManager.cs            # Ranking system
│       ├── RefereeController.cs      # Referee behavior
│       └── SceneLoader.cs            # Scene navigation
│
├── Scenes/
│   ├── Info.unity                    # Registration & ranking UI
│   ├── Beach.unity                   # Beach environment
│   ├── Beach_Night.unity             # Night beach
│   ├── Indoor.unity                  # Indoor stadium
│   ├── Outdoor.unity                 # Outdoor court
│   └── MainMenu.unity                # Main menu
│
├── Prefabs/                          # Reusable game objects
├── Models/Animations/                # Animation clips
├── Materials/                        # Shaders & materials
├── Brand/                            # Branding assets
└── XR/                              # XR-specific settings
```

## Key Implementation Details

### Bot AI State Machine Flow

```
Idle → Detect ball approaching
  ↓
PreparingHit → Move to predicted landing point, calculate target
  ↓
Hitting → Apply force to ball toward target
  ↓
ReturningToPosition → Move back to default position
  ↓
Idle (wait for next ball)

ServiceState (special case) → Ball toss animation → Hit on specific frame
```

### Ball Trajectory Prediction

The bot AI uses physics-based prediction in `BotController.PredictBallLandingPoint()`:
- Calculates landing point using projectile motion equations
- Uses average bot catch height as target plane
- Handles both successful reach cases and ground landing cases
- Critical for bot positioning and timing

### 3-Hit Rule Enforcement

Implemented in `BotController.GetRandomTarget()`:
- Tracks hit count per team (via VolleyballBall.cs)
- On 3rd hit: MUST select opponent target (farthest opponent)
- On 1st/2nd hit: Can select teammate or opponent
- Prevents rule violations automatically

### VR Hand Hit Dynamics

In `VRHandController.ApplyParabolicHitForce()`:
- **Slow (< 3 m/s)**: 55° angle → soft sets
- **Medium (3-6 m/s)**: 40° angle → controlled passes
- **Fast (6-10 m/s)**: 25° angle → aggressive hits
- **Very Fast (> 10 m/s)**: 15° angle → spikes
- Downward hand motion (velocity.y < -2) reduces angle for downward spikes
- Adds spin for realism

### Court Dimensions

Standard volleyball court (VolleyballCourtManager.cs):
- **Total**: 18m (length) × 9m (width)
- **Per side**: 9m × 9m
- **Net height**: 2.43m (men's standard)
- **Attack line**: 3m from net
- Procedurally generated at runtime

### Score and Match Logic

Match structure (GameManager.cs):
- Best of 5 sets
- Sets 1-4: First to 21 points (2+ point lead required)
- Set 5: First to 15 points (2+ point lead required)
- Rally scoring system (point on every rally)

## Debugging Tools

The codebase includes extensive debugging visualization:

### Gizmo Toggles (Inspector)
- **BotController**: `showTrajectory` - Shows predicted ball landing points and bot catch zones
- **VolleyballBall**: `showDebugInfo` - Displays hit count and ball state
- **VolleyballCourtManager**: `showDebugGizmos` - Renders court boundaries and zones

### Debug Workflow
1. Enable gizmos in Scene view
2. Toggle debug flags in Inspector
3. Monitor Console for state transitions
4. Watch bot behavior in real-time

### Common Debug Scenarios
- Ball not detected by bots → Check `CheckForBallInternal()` radius and layers
- Bot hitting out of bounds → Verify `CalculateLandingPointInternal()` calculations
- 3-hit rule violations → Inspect `GetRandomTarget()` logic
- VR hand not hitting ball → Check collision layers and `OnCollisionEnter()` in VRHandController

## Performance Considerations

- **Bot ball detection**: Uses `Physics.OverlapSphereNonAlloc` to avoid allocations
- **Static bot array**: Cached in BotController to reduce lookups
- **Gizmo guards**: Debug visualizations only draw when flags enabled
- **Frame-rate independence**: Physics calculations use Time.fixedDeltaTime

## Key Design Patterns

- **Singleton**: GameManager, PlayerDataManager (single global instance)
- **State Machine**: Bot AI behavior management
- **Observer**: GameManager events (OnScoreChanged, OnSetEnded, OnMatchEnded)
- **Strategy**: Hit force calculation varies by hand speed
- **Facade**: VRPlayerProxy simplifies VR system interaction

## Data Persistence

- **PlayerPrefs**: Temporary session data (current player info)
- **CSV Files**: Permanent player records at `Application.persistentDataPath/player_data.csv`
- **Format**: `Full Name, Phone, Email, Date, Time, Score`

## Scene Flow

```
1. Info.unity (Registration)
   ↓
2. Selected Game Scene (Beach/Indoor/Outdoor)
   ↓
3. Match Gameplay
   ↓
4. Match End → Return to Info.unity
   ↓
5. Show Rankings
```

## Common Tasks

### Adding a New Bot State
1. Create new class inheriting from `BotState`
2. Implement: `Enter()`, `Update()`, `FixedUpdate()`, `Exit()`
3. Add state transition logic in `BotController`
4. Update state machine diagram in documentation

### Modifying Court Dimensions
1. Edit constants in `VolleyballCourtManager.cs`:
   - `courtLength`, `courtWidth`, `netHeight`
2. Update bot positioning in `GameManager.SetupGame()`
3. Test ball drop zone detection in `VolleyballBall.cs`

### Adding New Game Scene
1. Create scene in `Assets/Scenes/`
2. Add to Build Settings (File > Build Settings > Add Open Scenes)
3. Update `SceneLoader.cs` with new scene name
4. Ensure GameManager and Court are present in scene

### Adjusting Bot Difficulty
Modify in `BotController.cs`:
- `movementSpeed`: Bot movement rate
- `reactionDelay`: Time before bot responds to ball
- Ball detection radius in `CheckForBallInternal()`
- Target selection randomness in `GetRandomTarget()`

## Critical Code Locations

- **Game orchestration**: `Assets/Scripts/Bot/GameManager.cs`
- **Bot AI logic**: `Assets/Scripts/Bot/BotController.cs`
- **Ball physics**: `Assets/Scripts/VolleyballBall.cs`
- **VR hand hitting**: `Assets/Scripts/VRHandController.cs`
- **Court setup**: `Assets/Scripts/VolleyballCourtManager.cs`
- **Player data**: `Assets/Scripts/PlayerDataManager.cs`

## XR Configuration

- Uses **Unity XR Interaction Toolkit** (2.6.4)
- **OpenXR** backend (1.14.3) for cross-platform VR
- Configured for Meta Quest optimization
- Settings: `Project Settings > XR Plug-in Management > OpenXR`

## Important Notes

- **BotController.cs** is 1420 lines - consider refactoring into smaller components if adding complexity
- **State classes** are very large (IdleState ~15k lines) - may need modularization
- Always test in VR headset for accurate hand tracking behavior
- Ball physics are tuned for specific gravity settings - changing Physics settings requires retuning
- CSV player data has no header row - maintain format when modifying
