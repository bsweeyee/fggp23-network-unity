# Requirements
## Game Design:
- Create a simple multiplayer game. Suggestions include:
- A basic top-down shooter.
- A simple racing game.
- A basic platformer.

The game should support at least two players (one host and one client).

## Networked Game Features:
1. Implement a client-server model.
2. Ensure player movement is synchronized across the network.
3. Synchronize at least one other game object (e.g., projectiles, NPCs).
4. Implement in game communication (e.g., chat messages, emotes).
5. Implement server authority to handle game state and synchronization.

# Design
Single lane PVP Unit defense. --> CLEAR #1
Objective: 
- Spawn enough units and get to the other side of the lane and win game

- LocalGame --> CLEAR #5
    - States ( NETWORK-VARIABLE )
        - WAITING
        - PLAY
        - END
- NetworkGame ( Player )
    - Spawn Unit
- NetworkUnit
    - States ( NETWORK-VARIABLE )
        - IDLE
        - MOVE --> CLEAR #2
        - ATTACK
- NetworkProjectile --> CLEAR #3
    - Spawn Projectile (Lightning?) on tap location
- NetworkCommunication --> Clear #4
    - Select a list of words
    - Tap word popup and sync message

# TODO
[o] Display Debug Logs on game builds ( to check for error in network ids etc.)
[o] Spawn Network Objects in their correct respective positions
    - OwnerClientId is probably always the same ( Belongs to Server in a Server authoritative system ). To get the id of the Client who RPC-ed, we need to send that information in the RPC itself
[o] Setup NetworkUnit States
[ ] Setup LocalGame States and handle destruction of objects accordingly
    - START
    - WAITING
    - PLAY
    - WIN
    - LOSE
[ ] When player hits spawn position, reduce player health and set to win / lose state depending on who's health falls below 0
[ ] Setup cooldown for each spawn
[ ] Prevent spawning if there is already a friendly unit at the start -> if implement merging, instantly merge
[ ] Implement units merging

# Documentation

## 06/08/2024
- Add spawning of NetworkGame -> NetworkGame will store the state of the Game 
- Add spawning of NetworkUnit

## 07/08/2024
https://stackoverflow.com/questions/67704820/how-do-i-print-unitys-debug-log-to-the-screen-gui

## 08/08/2024
0: 4.5,4,-5.5
1: -4.5,4,5.5

0: 30.00001, 320, -9.85853e-07
1: 30,140,0

## 09/08/2024
TODO:
- Test attacking and death state of Network Unit
- Handle Game State

## 10/08/2024
I'm not sure if OnNetworkSpawn goes first or OnClientConnected goes first
Based on local testing, OnNetworkSpawn triggers first, then OnClientConnected.
Why this is important is because I used the default spawned player object to handle things like adding NetworkGame(a player) to the master list.

TODO:
[o] Finish Game State handling
[o] Fix bug with player attacking
[ ] Handle game end loop