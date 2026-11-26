# Headcount - Breeding Pod Population Projection

How many beavers can your Iron Teeth breeding pods support?

## Iron Teeth

The `~N` shows where your population could settle over time, based on how often your pods are running and how long your beavers live. It includes both standard and advanced breeding pods.

Hover over this for more details.

## Folktails

The `/N` next to your population shows your bed count for the colony or selected district. Hover over it to see occupied and free beds, plus any beavers without a bed.

## How the breeding pod estimate is calculated

Steady state: population = birth rate × how long each beaver lives. 

- Births per pod come from the pod's `CycleLengthInDays × CyclesUntilFullyGrown`.
- Lifespan is the game's average lifespan, multiplied by the average `LifeExpectancy` bonus of your living adults.

### Interesting side notes
- Kits grow up in `DaysOfChildhood ÷ GrowthSpeed`. Their life clock keeps running during childhood, so adults born from kits live for the remainder of the lifespan.
- Each beaver's has a random ±10% longevity.

Available for Iron Teeth (and Folktails)
Requires Timberborn 1.1.0 or later.

## Changelog

### 0.7.0
- Fix for selecting any building would hide pop estimate