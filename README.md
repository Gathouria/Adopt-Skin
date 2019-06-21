# Adopt & Skin (Animal Skinner)

What A&S lets you do:

- Use a suite of your own custom sprites for any animal, pet, or horse in the game

- Uses a randomized skin by default when a new animal, pet, or horse appears

- Allows adopting multiple pets

- Allows adopting multiple horses

- Call a horse to you with a hotkey

- One-tile-wide horses, for galloping anywhere you damn well please

- Compatible with BFAV (Both 2.2.6 and the 3.0.0 beta)

- Compatible with save files with multipets or multihorses from another mod



# A&S does **NOT**:

- A&S doesn't come with the skins you need to use to customize skins. This is out of respect to sprite authors. You can download your own on Nexus. I recommend Elle's sprite replacements (cat, dog, horse, coop animals, barn animals)

- A&S lets you use multipet and multihorse save files created under other mods, but will need to uninstall those mods before using A&S. Results can vary otherwise.

- Other reskin mods will need to be uninstalled for A&S to work entirely as intended. But you can move these skins right into your shiny new `/assets/skins` folder (after naming them appropriately so that A&S can read them, as detailed below)



# To use custom sprites:

All farm animals, pets, and horses must be one of the following types (Note on BFAV animals below):

| **Animal Type** | **Baby subtype** | **Sheared subtype**|
|---|---|---|
| BlueChicken | *BabyBlueChicken* | - |
| BrownChicken | *BabyBrownChicken* | - |
| WhiteChicken | *BabyWhiteChicken* | - |
| VoidChicken | *BabyVoidChicken* | - |
| Duck | *BabyDuck* | - |
| Rabbit | *BabyRabbit* | - |
| Dinosaur | - | - |
| BrownCow | *BabyBrownCow* | - |
| WhiteCow | *BabyWhiteCow* | - |
| Goat | *BabyGoat* | - |
| Sheep | *BabySheep* | *ShearedSheep* |
| Pig | *BabyPig* | - |
| | | |
| | | |
| **Pet Type**| - | - |
| Cat | - | - |
| Dog | - | - |
| | | |
| | | |
| **Horse Type** | - | - |
| Horse | - | - |


* Sprite sheet files MUST be named in this fashion for A&S to parse them:

Examples: Dinosaur_1, BabyBlueChicken_1, BlueChicken_1, BabySheep_3, Sheep_3, ShearedSheep_3, etc.

* The creature Type written exactly as shown in the above type chart, no spaces
* The number following the file name is a unique identifying number for that type and skin (I.E. you can have *dog_1* and *cat_1*, but not two *cat_1*s)
* A&S MAY BREAK AND ACT ODDLY when setting skins if an animal type's sprite sheets are not numbered starting at 1 or not numbered continuously
(I.E. do not have only two skins and have their IDs as 1 and 7. Just do 1 and 2 like a reasonable person.)
* Animals that look different as babies or when sheared/harvested from must have a separate sprite sheet, named with the same identifying number, and have this name be proceded by "Baby" or "Sheared" (I.E. BabySheep_1, Sheep_1, and ShearedSheep_1 all make a single sheep skin set)
* All sprite files must be of type .PNG or .XNB
* All sprite files must be placed in `AdoptSkin/assets/skins`


# BFAV Compatibility

BFAV-added animals must have their skin files named in the same format as other farm animals/pets/horses, with the type name seen in the chart above instead being the name as it is written in the BFAV `Config` file.

**ADDITIONAL BFAV NOTE:**
If you're using a BFAV animal with color variants and would like the functionality of additional custom sprites that A&S adds, note that you will have to do the whole skin set for *each* color variant.
I.E. BFAV adds a red seagull and a blue seagull. If you want them both to rotate through the same three skins, you will have to make your three skin files say *blueseagull_1*, *blueseagull_2*, and *blueseagull_3*, alongside any blue/sheared sprites, then copy these skins and rename them to have an identical set that is *redseagull_1*, *redseagull_2*, and *redseagull_3*.

If you know what you're doing (a.k.a I am not user support for BFAV, and am also not responsible for broken code) then you can make a new animal class of just "seagull".

---

# FAQ

**`list_animal` isn't working anymore**
> Commands were changed in 2.X. Call `help` in the SMAPI console to see what the new commands are

**Does the horse whistle work with the tractor mod?**
> Yep.

**Can I turn off wild horses/pet strays and just use the reskinning?**
> Yes, this can be configured in the `Config` file, which shows up in the A&S mod files after you've run SMAPI at least once after installing A&S.

**I can't find any wild horses!**
> Wild horses are somewhat uncommon, and they have about a 25% chance to appear somewhere on the map each day. They're intended to be an exciting find, rather than something you get bored of running across. That said, if you'd like to know when and where a horse appears, set `DetailedConsoleOutput` to `true` in A&S's `Config` file.
