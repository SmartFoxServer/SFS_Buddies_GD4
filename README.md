# SmartFoxServer Example Projects for Godot 4.x

This series of **C# examples** built for the Godot 4 engine have been developed with **Godot Mono 4.0.3**, but the concepts and the code to interact with the SFS2X API are valid for any version of Godot 4.x (unless otherwise noted).

Each of the tutorials in this series examine a single example, describing its objectives, **offering an insight into the SmartFoxServer features** it wants to highlight. This project includes all the assets required to compile and test the example (both client and — if existing — server side). If necessary, code excerpts are provided in the tutorial itself (see online documentation link below), in order to better explain the approach that was followed to implement a specific feature. At the bottom of the tutorial, additional resources are linked if available.

The tutorials have an increasing complexity, from basic server connection to a complete game with authoritative server code.

Specifically, the examples will showcase:

* basic connection with optional protocol encryption
* room management
* **buddy list management**
* game rooms and match-making
* authoritative server in a turn-based game

The Godot examples provided have been tested for exporting as native executables for Windows and macOS. At the time of writing this article (June 2023) Godot Mono does not yet support exporting for mobile platform or the browser.

# SFS_Buddies_GD4
This example is the second in a series of three in which we lay the foundations for a lobby application to be used as a template in multiplayer game development. A lobby is a staging area which players access before joining the actual game. In a lobby, users can usually customize their profile, chat with friends, search for a game to join or launch a new game, invite friends to play and more.

This example expands the one described in the Lobby: Basics tutorial by adding a friends list to the Lobby scene, with icons to represent the state of friends, controls to add, remove and block them and a panel to exchange private messages. Additionally users can access a profile panel to set their state and other details that will be visibile to their friends.


<p align="center"> 
<img width="720" alt="connector-login" src="https://github.com/SmartFoxServer/SFS_LobbyBasics_GD4/assets/30838007/3001b9ec-4cc0-4316-9fe2-eaeecdc60a60">
 </p>h

In this document we assume that you already went through the previous tutorial, where we explained the subdivision of the application into three scenes, and how to create a GlobalManager class to share the connection to SmartFoxServer among those scenes.



## Setup & run
In order to setup and run the example, follow these steps:

1. unzip the examples package;
2. launch the Godot, click on the Import button and navigate to the Connector folder;
3. click the **Build button** in the top right corner of the Godot editor before running the example;

The client's C# code is in the Godot project's *res://scripts* folder, while the SmartFoxServer 2X client API DLLs are in the *res:// folder*.

## Online Tutorial and Documentation
The base code for this example is the same of the previous one, expanded to implement the new features.

The LobbySceneManager and GameSceneManager classes have been updated to add the logic related to the Buddy List management and interaction. Also, in the scripts subfolder, the new BuddyChatHistoryManager static class provides helper methods and cross-scene data sharing for the buddy chat, as discussed below.

<p align="center"> 
<img width="720" alt="connector-login" src="https://github.com/SmartFoxServer/SFS_LobbyBasics_GD4/assets/30838007/c559cfd9-d0af-41f4-866e-65323a279c25">
 </p>

To learn more about this template and how it is configured for establishing a connection and handling basic SmartFoxServer events, go to the online documentation and tutorials linked below.

**SmartFoxServer Example Documentation**   

http://docs2x.smartfoxserver.com/ExamplesGodot/lobby-basics

This online documentation includes:
* Buddies List Initialization
* The User Profile
* Add, Remove, and Block Buddies
* Exchanging messages with buddies
  
 and further **Resource Links**

http://docs2x.smartfoxserver.com/ExamplesGodot/introduction

 <p align="center"> 
<img width="400" alt="connector-login" src="https://github.com/SmartFoxServer/SFS_Connector_GD4/assets/30838007/a8f025fb-5bc0-4ca6-8ce0-8ec808565303">
 </p>

