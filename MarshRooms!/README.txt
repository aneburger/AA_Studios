BELANGRIK OM TE WEET:
So die resolution is 480 x 270 soos jy weet en ek het nou screen effects by gesit (vir die hurt effect, maar dis net n placeholder rooi blok vir nou)
as dit lyk asof dit afgesny word dan in game view kan jy n custom resolution add (ek weet nie of myne op git gaan wees nie want user setting is .gitignore..)
Anyways ek dink net dis belangrik vir die HUD dat jy presies kan sien waar al die elements gaan sit. 
Ook as jy dit op 480 x 270 render lyk die rotated pixels weird maar dit lyk darem nie so as jy dit build nie
<3

Hellooo, hier is wat jy solank aan kan werk, laat weet my as iets nie sin maak nie :)

Sound Effects:
- Dodge - goes in PlayerMover
- Pistol
- Minigun
- Hit enemy / hurt player - goes in PlayerHealth and EnemyHealth by overriding TakeDamage() so they can sound different (I might be wrong so just check)
- Weapon equip - goes in BaseShooter, plays when switching or picking up a weapon
- Anything else you can think of


To add the weapon sounds:
Just go to Scripts > Combat > Weapons, open the weapon scriptable object and drag your clip into the Shoot Clip field. Nothing else needed. Remember to adjust the volume I forgot once and it was tragic


Adding other sounds:

Please add this header to whichever script needs it:

[Header("Audio")]
[SerializeField] private AudioClip clipName;
[Range(0f, 1f)] public float clipVolume;

Then play it using AudioManager:
You can use either PlaySFX or PlaySFXWithPitch(if you think it sounds better with variation)


UI / HUD
Health display - we can do classic pixel art hearts or something mushroom themed? Up to you
Current weapon indicator - maybe an icon or name? all the weapon info like the name is in WeaponData (I am still working on adding an ammo count so if you want to add a temporary thing for that you can) Also the PlayerWeaponSlot keeps track of the current weapon
You can find a cool Pixel art font for all menus
Pause menu - press Escape
Main menu - just a simple start button for now?

Importing Sprites
Every sprite needs these exact settings in the Inspector:

Pixels Per Unit - 16
Filter Mode - Point (no filter)
Compression - None

File Organisation
All sprites go in the Art folder under the relevant subfolder
Naming convention I used (or tried): capital letters for folders, hyphens for spaces
I think you'll mostly be working in the UI subfolder for now so feel free to organise that however makes sense to you :)

Ek dink dis al jy kan die file delete as jy klaar is, ek hoop hierdie help darem
