<p align="center">
  <a href="https://gangland.vip">
    <img src="https://raw.githubusercontent.com/ganglandvip/ganglandvip/main/gangland.banner.png" alt="Gangland VIP banner" width="100%">
  </a>
</p>

<h1 align="center">Gangland.vip</h1>

<p align="center">
  <strong>A persistent VR crime world where territory, deception, reputation, and money keep moving after every session.</strong>
</p>

<p align="center">
  <a href="https://gangland.vip"><img alt="Website" src="https://img.shields.io/badge/Website-gangland.vip-f6c85f?style=for-the-badge"></a>
  <a href="https://x.com/ganglandvip"><img alt="X" src="https://img.shields.io/badge/X-@ganglandvip-111111?style=for-the-badge&logo=x"></a>
  <a href="mailto:info@gangland.vip"><img alt="Contact" src="https://img.shields.io/badge/Contact-info%40gangland.vip-c64f4f?style=for-the-badge"></a>
  <img alt="Status" src="https://img.shields.io/badge/Status-In%20Development-2ea043?style=for-the-badge">
</p>

<p align="center">
  <a href="#the-world">The World</a> |
  <a href="#core-systems">Core Systems</a> |
  <a href="#current-focus">Current Focus</a> |
  <a href="#repo-map">Repo Map</a>
</p>

---

## The World

**Gangland** is an open-world multiplayer VR crime simulator set inside a living city inspired by South Central Los Angeles. It is built around persistence: crews gain and lose territory, businesses generate money, reputations spread, weapons move through the streets, and player decisions keep shaping the world after a session ends.

This is not a short-round shooter with a reset button. It is a social, economic, and territorial sandbox where the best content comes from the people inside it.

## Setting

**Gangland** takes place in a realistic recreation of **South Central Los Angeles**, centered around **3420 W. Slauson Avenue, Los Angeles, CA 90043** (the Crenshaw & Slauson area).

The game uses real-world map and geographic data to accurately recreate the streets, intersections, landmarks, buildings, terrain, and overall neighborhood layout. The goal is an authentic, living open-world environment inspired by the real area while remaining a fictional game world.

## Core Systems

| System | What Players Feel |
| --- | --- |
| Persistent territory | Crews fight for neighborhoods, businesses, warehouses, clubs, and income streams. |
| Social deception | Undercover cops, informants, witnesses, lawyers, journalists, and double-crosses create unscripted drama. |
| Role freedom | Players can become gang members, detectives, civilians, store owners, hitmen, EMTs, police, or something nobody planned for. |
| Long-term stakes | Money, reputation, weapons, properties, vehicles, and relationships can be built, protected, stolen, or lost. |
| Living economy | Legal and illegal businesses give players reasons to negotiate, protect, rob, investigate, and betray. |

## Player Roles

<table>
  <tr>
    <td><strong>Street Power</strong></td>
    <td>Gang members, crime bosses, drivers, enforcers, gun dealers, bank robbers, hitmen.</td>
  </tr>
  <tr>
    <td><strong>Law & Pressure</strong></td>
    <td>Police, detectives, undercover officers, informants, witnesses, lawyers, journalists.</td>
  </tr>
  <tr>
    <td><strong>City Life</strong></td>
    <td>Civilians, store owners, club operators, taxi drivers, EMTs, business partners, rivals.</td>
  </tr>
</table>

Nobody is locked into one gameplay style. The city supports both legal and illegal paths, and every role can matter.

## Why Persistence Matters

Many VR multiplayer shooters reset after short rounds. Gangland is built so the city continues:

- Gangs gain and lose territory.
- Businesses generate money over time.
- Weapons are bought, sold, hidden, and seized.
- Reputations grow through what players actually do.
- Betrayal matters because trust has value.
- Police raids matter because something real can be lost.

Today you might be a nobody. A week later, everyone might know your name because your crew controls a block, a club, a supply route, or a court case.

## Current Focus

- Real-world city data pipeline for believable streets, buildings, intersections, landmarks, and terrain.
- Unity-based VR client with streamed city chunks and dense urban environments.
- Persistent multiplayer systems for crews, territory, economy, player identity, and consequences.
- A cinematic, street-level tone driven by player choices instead of scripted missions.

## Repo Map

| Path | Purpose |
| --- | --- |
| `gangland-client/` | Unity VR client project and gameplay systems. |
| `gangland-server/` | Server-side gameplay, persistence, inventory, combat validation, and player state. |
| `gangland-backend/` | Accounts, authentication, admin, store, statistics, leaderboards, and social systems. |
| `shared/` | Shared constants, protocols, and message schemas. |
| `utility-folder/` | OSM and map-data pipeline for the Slauson-area city build. |

## North Star

Build a persistent VR crime world where every player has something to gain, something to hide, and something to lose.

<p align="center">
  <a href="https://gangland.vip"><strong>Visit gangland.vip</strong></a>
  &nbsp;|&nbsp;
  <a href="https://github.com/ganglandvip/ganglandvip"><strong>Explore the repo</strong></a>
  &nbsp;|&nbsp;
  <a href="https://x.com/ganglandvip"><strong>Follow @ganglandvip</strong></a>
</p>
