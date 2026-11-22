# Headcount

A Timberborn mod that shows how many beavers your breeding pods can support, or how many beds you have.

## What it shows

### Iron Teeth (breeding pods)

A `~N` figure is added to the end of the population row (top left). Hover it for details:

- Expected adults and kits, next to the current counts.
- Pods running, idle (missing nutrients or blocked) and paused, plus how many are Advanced Breeding Pods.
- Pod uptime over the last few days, and the projection if every unpaused pod keeps running.

The figure is colony-wide, so it is hidden while a district is selected. It only appears once you have a finished breeding pod.

### Folktails (housing)

Folktails need space at home to have kits. A `/N` figure shows the number of beds and follows the panel's district selection. Hover it to see which beds are taken or free, and how many beavers don't have one.

## How the breeding pod estimate is calculated

Steady state: population = birth rate × how long each beaver lives. Everything is read from the game rather than hardcoded, so well-being tiers, difficulty and other mods are taken into account.

- Births per pod come from the pod's `CycleLengthInDays × CyclesUntilFullyGrown`.
- Lifespan is the game's average lifespan, multiplied by the average `LifeExpectancy` bonus of your living adults. Each beaver's random ±10% longevity averages out.
- Kits grow up in `DaysOfChildhood ÷ GrowthSpeed`. Their life clock keeps running during childhood, so adults born from kits live for the remainder of the lifespan.
- Advanced Breeding Pods produce adults with a full adult lifespan and no kit phase.
- Uptime is a moving average of the time each pod spends growing, measured in game time.

With base stats, one 5-day pod gives about 10 beavers.

It is a ceiling: starvation, thirst and injuries keep the real number lower. Changes in well-being take about one lifespan to show up in the population.

## Requirements

Timberborn 1.1.0 or later.

## Building

Built with the [Timberborn modding tools](https://github.com/mechanistry/timberborn-modding). Place this folder in `Assets/Mods/` and build with the Mod Builder.
