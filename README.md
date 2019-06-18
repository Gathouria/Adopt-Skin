# AnimalSkinner
A mod for Stardew Valley, enabling multi-pet and multi-horse adoption, alongside reskin abilities
To use custom sprites:

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

** BFAV-added animals must be named in the same format, with their name being the same as it looks in the BFAV config file.

** Sprite sheet files MUST be named in this fashion for Animal Skinner to parse them:

Examples: Dinosaur_1, BabyBlueChicken_1, BlueChicken_1, BabySheep_3, Sheep_3, ShearedSheep_3, etc.


* Type of animal written exactly as shown in the above type list, no spaces
*  The number following the file name is a unique identifying number
*  Animals that look different as babies or when sheared/harvested from must have a separate sprite sheet, named with the same identifying number, and have this name be proceded by "Baby" or "Sheared" (eg BabySheep_1, Sheep_1, and ShearedSheep_1 all make a single sheep skin set)
* ANIMAL SKINNER MAY BREAK AND ACT ODDLY when setting skins if an animal type's sprite sheets are not numbered starting at 1 or not numbered continuously
(eg, do not have only two skins and have their IDs as 1 and 7. Just do 1 and 2 like a reasonable person.)
* All sprite files must be of type .PNG or .XNB
* All sprite files must be placed in Animal Skinner/assets/skins


**ADDITION BFAV NOTE:**
If you're using a BFAV animal with color variants and would like the functionality of additional custom sprites that Animal Skinner adds, note that you will have to do the whole skin set for *each* color variant.
I.E. BFAV adds a red seagull and a blue seagull. If you want them both to rotate through the same three skins, you will have to make your three skin files say *blueseagull_1*, *blueseagull_2*, and *blueseagull_3*, alongside any blue/sheared sprites, then copy these skins and rename them to have an identical set that is *redseagull_1*, *redseagull_2*, and *redseagull_3*.

If you know what you're doing (a.k.a I am not user support for BFAV, and am also not responsible for broken code) then you can make a new animal class of just "seagull".
