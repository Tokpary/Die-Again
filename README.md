#Die Again

**Die Again** is a dice-based roguelike developed for the Unity 2026 Game Jam. Your goal is to hit the **Lucky Number** by throwing and rerolling the dice you own. Along the way you can buy new dice, collect cursed objects that change your run, and even face the Sins: bosses that will try to make your rounds harder.

The game is built on scalable, modular mechanics, and its ScriptableObject-based architecture makes it easy to add new content.

## Dice

Dice are the core mechanic of the game. Each die has three main attributes:

- **Shape:** defines how many faces the die has. It can be a coin, a cube, a pyramid…
- **Face values:** each face holds a value that contributes to your final score in some way.
- **Material:** each material changes how the die behaves physically. **Glass** shatters when it hits another die, and **antimatter** is destroyed on contact and takes the other die with it.

## Cursed Objects

Cursed objects are passive modifiers that change the rules of your run, such as increasing the Lucky Number tolerance or the number of throws and rerolls per round.

## Bosses

Every week a Boss is waiting for you, and the chance of facing it increases each day. Boss fights add a special win condition to the round: for example, all your dice turn into glass, or rerolls are disabled.
