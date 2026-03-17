# DM_AmuletBedSpawn

This is a mod for the game Vintage Story.

## QUICK DESCRIPTION

This mod will allow the player to set their spawn point by sleeping in a bed, but only if they are wearing a Temporal Gear Amulet or a Rusty Gear Amulet. I've also included a user-editable config file that allows players to set whether they would like their worn amulet to have a percentage chance to break upon every respawn along with a few other options to further define balance. See below...

## WHY?

I installed some of the other bed spawn mods and enjoyed them, but it was just a little TOO easy in the very early game. All you had to do was gather some grass to make a Hay Bed and boom, you set your spawn. This mod makes sure that you still keep a little bit of tension in the very early game. If you want the comfort of a nice spawn point to come back to, you've got to do a bit of hunting for a rusty gear at the minimum. Maybe you'll get lucky and find one laying on the ground, but often times it could be an hour or two before the first one is organically found. And to me, that tiny bit of exploration tension is fun.

## DETAILS

If a **Rusty** or **Temporal Gear Amulet** is not worn, the bed functions normally -- you just sleep.
If the bed is destroyed or moved, any player that has set their spawn point on that bed will have their spawn point reset to the default world location.
A message will be displayed within the chat window if the spawn is successfully set and also if the spawn gets reset due to a bed being broken.
There are a few options that can be changed via config file. See below.

## OPTIONAL CONFIG FILE (starting with version 1.0.2)

There is a config file (created after the mod is first run) called **AmuletBedSpawn.json** and it is located in the .../VintagestoryData/ModConfig/ folder.

On creation, it looks like the following:
```
{
"AllowHayBeds": true,
"AllowRustyGearAmulets": true,
"AmuletsCanBreakAfterRespawn": false,
"RustyGearAmuletBreakChancePct": 30,
"TemporalGearAmuletBreakChancePct": 5
}
```
"**AllowHayBeds**" is defaulted to "true". If set to false, hay beds cannot be used to set a player's spawn (regardless of the type of amulet being worn).

"**AllowRustyGearAmulets**" is defaulted to "true". If set to false, rusty gear amulets cannot be used to set a player's spawn (only temporal gear amulets can be used).

"**AmuletsCanBreakAfterRespawn**" can optionally be set to "true". If it is true, the following game rules go into effect:
 
After you've set your spawn with a Rusty Gear Amulet or a Temporal Gear Amulet, you must be wearing that amulet when you die to be respawned back to your bed.
If you aren't wearing one of the approved amulets when you die, your spawn point will be removed, and you will be respawned at the default world location.
If you set your spawn with a Rusty Gear Amulet, but then later on replace it with a Temporal Gear Amulet, you are not penalized. That works, and it's a good thing.
When you respawn back at your bed, your amulet will now have a chance to break and be destroyed completely. If the amulet is destroyed, so is the spawn setting. So, you'll need to find/make a new amulet and reset your spawn. The percentage chance of that happening is defined by modifying the other options in the config file.
 
"**RustyGearAmuletBreakChancePct**" can be set to an integer value between 0 and 100. The default value of 30 means that a Rusty Gear Amulet has a 30% chance of breaking after respawning at your bed.

"**TemporalGearAmuletBreakChancePct**" can be set to an integer value between 0 and 100. The default value of 5 means that a Temporal Gear Amulet has a 5% chance of breaking after respawning at your bed. This means, you will eventually want to craft one of these to lessen your breakage chance!
 
## HANDY VANILLA RECIPES

**Rusty Gear Amulet** : Made by placing 1 coil of rope above 1 rusty gear in the crafting window. It must then be worn by pressing 'C' to bring up the Character UI and then placed into the top right necklace slot.

**Temporal Gear Amulet** : Made by placing 1 coil of rope above 1 temporal gear in the crafting window. It must then be worn by pressing 'C' to bring up the Character UI and then placed into the top right necklace slot.
