# Barrel Dash 🏃‍♂️💨

[![Play Web Demo](https://img.shields.io/badge/Play%20Web%20Demo-Click%20Here-success?style=for-the-badge&logo=google-chrome&logoColor=white)](https://mseymur.github.io/)
![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Kinect](https://img.shields.io/badge/Kinect-v2-blueviolet?style=for-the-badge&logo=microsoft)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Web%20%7C%20Mobile-lightgrey?style=for-the-badge)

**Barrel Dash** is an immersive, Kinect-powered infinite runner where you step into the shoes of a mischievous goblin racing toward a castle. Built with Unity, this project explores full-body gesture control to create a unique arcade experience.

> **Note:** This project includes a Kinect-powered immersive version, a touch-based mobile build, and a [WebGL version](https://mseymur.github.io/) playable in your browser.

---

## 🎮 Gameplay

In **Barrel Dash**, traditional controllers are replaced by your own body. Using a **Microsoft Kinect v2** sensor, the game tracks your movements in real-time:

*   **Steer:** Lean left or right to navigate the goblin through the winding path.
*   **Turn:** Rotate your shoulders to make sharp turns.
*   **Dodge:** React quickly to avoid obstacles like fences and spike traps.
*   **Collect:** Grab coin bags to boost your score as you race against time.

The goal is simple: Run as far as you can, collect as much loot as possible, and don't get caught!

## ✨ Features

*   **Immersive Controls:** Custom-built gesture recognition pipeline optimized for the Kinect v2.
*   **Dynamic Calibration:** "T-Pose" calibration system to adapt to different player heights and room setups.
*   **Multi-Platform:** 
    *   **Kinect Mode:** The full physical experience (Windows).
    *   **Web/Mobile Mode:** Optimized for keyboard and touch controls.
*   **Vibrant World:** A fairytale-inspired environment built with custom assets and shaders.
*   **Real-time Feedback:** Visual cues and UI elements that respond to player movement to reduce motion sickness and input lag.

## 🛠️ Tech Stack

*   **Engine:** Unity
*   **Languages:** C#, C++ (for native plugin integration)
*   **Hardware:** Microsoft Kinect v2
*   **Design Tools:** Blender (3D Modeling), Figma (UI/UX)
*   **Middlewear:** Custom Kinect wrappers for Unity to handle skeletal tracking and gesture smoothing.

## 🚀 Getting Started

### Prerequisites

*   Windows PC (required for Kinect SDK)
*   Microsoft Kinect v2 Sensor + Adapter
*   [Kinect for Windows Runtime 2.0](https://www.microsoft.com/en-us/download/details.aspx?id=44561)
*   Unity 2021.3 LTS or later

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/Mseymur/Barrel-Dash.git
    ```
2.  **Open in Unity:**
    *   Launch Unity Hub.
    *   Add the `Barrel-Dash` folder as a project.
    *   Open the project (wait for assets to import).
3.  **Setup Kinect (Windows only):**
    *   Ensure your Kinect is plugged in and recognized by Windows Device Manager.
    *   Open the 'KinectView' scene (or Main Menu scene) to test the sensor connection.

## 🕹️ Controls

| Action | Input (Kinect) | Input (Web/Keyboard) | Input (Mobile) |
| :--- | :--- | :--- | :--- |
| **Move Left** | Lean Body Left | `A` or `Left Arrow` | Swipe Left |
| **Move Right** | Lean Body Right | `D` or `Right Arrow` | Swipe Right |
| **Turn** | Rotate Shoulders | `A`/`D` (at corners) | Swipe |
| **Jump** | *(Auto/Scenario dependent)* | `Space` | Swipe Up |

## 👥 The Team

Created in 1.5 months as a collaborative development project.

*   **Seymur Mammadov** - Developer & Engineering
*   **Alice Zanutti** - Design & Art
*   **Felix Prince** - Design

## 📄 License



---

*[Learn more about the project development here.](https://mseymur.framer.website/projects/barrel-dash)*
