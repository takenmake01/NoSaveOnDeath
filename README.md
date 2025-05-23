# **No Save on Death Mod**

This mod makes it so that when you die in raid, your progress is not saved. This means that in the event that you die, go MIA, or quit the raid through the menu, everything including gear, experience, and quest progress is restored to how it was before you went on the raid.

The goal of this mod is to cater to those who want to experience Tarkov in the style of a classic singleplayer game with saving/loading. Only there are no checkpoints within a Tarkov raid, so you have to actually survive to keep anything you find and progress in your quests. This means your secure container will function like any other slot in your inventory, and won't offer you any post-mortem assurances. It also means that when you kill tough enemies or complet challenging quest objectives, you have to survive the raid to lock in the progress (**Again, Run Throughs are fine and your progress will be saved!**).

This is my first mod, but it's quite simple so I don't expect any odd behavior or bugs on its own. Working with other mods that modify the InraidController.ts (specifically the savePostRaidProgress method) may break this.

**NOTE:** Items you bring into the raid with you will lose any Found In Raid (FIR) status during the pre-raid save, so don't count on this mod to protect you from accidentally bringing in something that was FIR that you wanted to *stay* FIR! Because it won't.