ChatPlex SDK

This SDK was created to simplify and streamline mods creation for multiple games. It mostly provides generic components and utilities for Unity modding.

Each mods using ChatPlexSDK can declare a module (optional, described below), this module is like an interface for the SDK to manage your mod, show settings UI and a enable/disable toggle if the module `Type` is defined as `Integrated`

Components (Full list in documentation):
- **CP_SDK** *ChatPlex SDK game agnostic namepace*
  * **Animation** *Animated image loading and processing*
  * **Chat** *Chat service for connecting to various live streaming platforms chat*
  * **Config** *Json configuration utilities*
  * **Logging** *Logging utilities*
  * **Misc** *Quality of life utilities*
  * **Network** *Network and HTTP utilities*
  * **Pool** *Memory management & pools utilities*
  * **UI** *User interface components, views, flow coordinator, builders and factories*
  * **Unity** *Tools and extensions to interact with Unity on different layer & threads, load fonts/sprites/textures*
  * **Utils** *Various platform utils like Delegate/Action/Function/Event system*
  * **XUI** *Tree like syntax CP_SDK::UI builder*

Todo...