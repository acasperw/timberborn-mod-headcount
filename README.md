# Headcount – Breeding Pod Population Estimator

How many breeding pods do your Iron Teeth need? Headcount estimates how many beavers your current pods can sustain and shows the answer beside your population counter.

A `~N` figure is added to the end of the population row

The figure is colony-wide.
It only appears once you have a finished breeding pod.

## How the breeding pod estimate is calculated

Steady state: population = birth rate × how long each beaver lives.

- Births per pod come from the pod's `CycleLengthInDays × CyclesUntilFullyGrown`.
- Lifespan is the game's average lifespan, multiplied by the average `LifeExpectancy` bonus of your living adults. 

- With base stats, one 5-day pod gives about 10 beavers, and grows with wellbeing bonus's

### Interesting side notes
- Kits grow up in `DaysOfChildhood ÷ GrowthSpeed`. Their life clock keeps running during childhood, so adults born from kits live for the remainder of the lifespan.
- Each beaver's has a random ±10% longevity.