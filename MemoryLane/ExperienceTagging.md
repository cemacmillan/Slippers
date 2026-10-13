In RimWorld, nearly all of my efforts tend to be complex non-linear systems which tend to a homeostasis reflecting facts on the ground, vs. pawn and where possible, groupings of pawns, react and develop to how it is, vs. how they _think it could be_.

Most interact through Mind Matters, which use pawn experience to shape pawns through acquisition of (Dynamic) Needs, Traits, and the collected _pool_ of their individual, and, where possible, collective Experiences. Experience is a Mind Matters (MM hereafter) concept we've introduced, like Dynamic Needs.

Experiences are constructed with a "payload" of Tags and a Valency, which so far includes things like:

public enum ExperienceValency
{
    Positive,     // Explicitly good events (compliments, achievements, gifts)
    Negative,     // Explicitly bad events (insults, failures, trauma)
    Neutral,      // Neutral events (routine work, non-controversial ceremonies)
    Eldritch,     // Mysterious, unsettling, or reality-altering events
    Affirming,    // Experiences reinforcing norms, roles, or ideoligions
    Humiliating,  // Events that degrade dignity, self-worth, or social standing
    Exhilarating, // Thrilling, adrenaline-pumping experiences (victories, near misses)
    Transformative // Events that fundamentally change the pawn's self-concept
}

So far, there's no concept of _weight_ to an Experience - we're not sure it's necessary since every experience is impactful, yet, potentially meaningless in the game unless circumstances make it meaningful, by evoking or re-awakening Needs.

Our mod Positive Interactions is a "typical" client of MM. It uses a _lossy_ mechanism to drop the majority of Experiences which occur to a pawn, modulated by the Consciousness stat of the patient pawn.

Thus, a pawn already entubated in a hospital bed is somewhat non-plussed to learn that two more colonists died, on an Experience level, in probabilistic terms, due to the lossy _probability_ which Positive Connections uses to refer Experiences to Mind Matters.

OTOH, it's just possible that there in their desperate state when the player has another pawn strip them to preserve their valuable Apparel if they die from being tainted, _this_ is the Experience that will get through to their 0.15 Consciousness brain and stick with them when they wake up. Not all the deaths, not all the people who cared for them, wishing for their recovery, etc. That they were given up for dead by the colony. 1/2000 chance, off the cuff, for a 0.15 Consciousness pawn, plus the unlikely possibility they only remember being rescued, cared for, as ThoughtMemories, with the games usual dismissal after _x_ days.

Experiences, are different. They stick and become part of a bitmap that pawn carries around with them indefinitely. This makes storing and testing for them _highly efficient_.

BTW, I just extended our design a little there... it's not _quite_ that developed, so let's add this point to our game plan: we need to further think about PawnExperienceMaps. They _exist_ but we are not using them in MM with quite this degree of sophistication.

This _is_ how we've modeled Dynamic Needs however. Each Need, a bitmap with affected pawns, each pawn, a bitmap of Needs, a bitmap of each category of pawn Needs, called a _pawnNeedsMap_. Thus, a map for "ordinary" Needs which go beyond the basic ones provided by game, FormalityNeed, being an example, is part of the main map of Needs. Like ValidationNeed, it is quite commonplace in generated pawns due to Traits and Backstories which produce the Need. Need Maps for pawns thus might be pawnPhobiaMap, pawnQuirkMap, etc.

Maps can operate independently but NeedsMgr (which coordinates Needs stuff) pays attention to epidemiology. We don't want to overwhelm the player with a hundred pawns with 100 different new pawn Needs to contend with. Rather, PawnNeedStatus allows a Need to disappear when it's no longer relevant. Player will be warned of dangerous Needs via Letter. ie. WitnessBloodNeed is fine in a triage responder or butcher, not so fine in a master crafter of shoes.

Now, I'd like us to think about what I've just described as our "caps" so to speak - extra tools we have beyond what the game provides:

- Interactions which can drive Experiences. We are masters of efficient use of InteractionWorkers.
- Witness, the in game method but also our extensions of it, which can drive Experiences and affected Needs such as Phobias.
- Start of methods to reflect Experiences into Dynamic Needs and MM Mental States, which are _softer_ non-blocking Mental Breaks essentially which allow a pawn to partially function, while changing priorities and what they will or will not do.
- Accidents, an entire Mod which provides Accidents as a category and can drive Experiences and impact pawns deeply.
- Slippers, an entire Mod which provides Experiences for wearing (or wielding for that matter) items or interacting with them, in a Thing Satisfies/Provokes Need model which is thoroughly generalized.

So, we can imagine that a pawn has an Experience which sticks, which evokes a (probably transient) Need, or, quasi-permanent Trait, or, a particular oddball mental state: Must stare at Dance Ball, might be a funny one we'd introduce to get more out of Ideology DLC addons like the named building which allows for the Dance Party ritual, but is otherwise unused.

Mental States in RimWorld are _cheap to use_ and well implemented like Stances supporting a cladistic pattern. A pawn can be Busy and Laying at the same time, if their job is Laying In Bed, Lovin' etc.

So for Mental States but, absolutely no one uses them this way.

Blurrier Stuff: We know the game has infrastructure for a Moon and things which are in space above the map. Using standard 60 day year cycles and a Rotator class, we can cheaply simulate longer term celestial phenomena I think.

I was imagining this as a RareInteraction in Positive Connections. Conditions: two pawns, not DirectRelation or Lovers, idle beneath nighttime sky without artificial lighting beyond campfire. If the conditions are right, we test if one of the pawns at least, has a memory of a pawn who is lost to WorldPawn status, or, dead.

Failing this, we might check for other relevant ThoughtMemories, and pick _something_ that looks like it belongs in the Story of the Interaction.

Initiator will text generate, using game methods, our extensions to them, and Translate() always:

"Staring up at the ...|Considering the ...|... {CelestialBody|Stars|...} {initiator} remarks: I've {MemorySubject.OBJ} on my mind."

Recipient will do:

"{recipient} pointing says: Look at the {GeneratedObjectOfInterest}. Doesn't it make you think of {MemorySubject.(some tag to make it fit remark)} ..."

The message then appears that the two contemplate a while together. This can then flag this pair, for a secondary knock-on Interaction, as well as usually induce positive opinion boni and memories. Pawns with certain Traits could of course, react negatively or mediate the Interaction according to their Traits, Needs, etc.

'We have absolute power...'

Just joking, but, it is a game, and the vacant lot of Core is there for us to build upon, with selective borrowings from DLC, trying not to block without it, but extending if it is present.

To realize the above, you can see we'd need infrastructure added on to the game. A Rotator-based system to guestimate positions of important to this game, celestial objects, which would use seasonality and game concept days of year as primary inputs.

Based on lore, and standard possible conditions, there is one sun. No rules state there is one moon, and mods like Moonlight and Perfect Darkness assume there may be more than one, and pay attention to the game primitives which apparently, model this.

So, we know these are things in the game source we can dust off and use somewhat as is, and other mods will work with them, at least the named ones.

Finally, to put our backs into it. Let's create an ecosystem perhaps, for the following Memes, using all the stuff we have to work with, and, assuming it's possible a meditation focus and type called Adoration, also exists, if player has also loaded AOSMeditation. Just think of it as veneration or adoration, personal presence as a Meditation act, and you understand what it does. That's it. :) Pawns can become Retainers or VeneratingServants  (like priests or temple slaves) to other pawns. AOSM is _very_ extensible, as its very primitive to these concepts and doesn't add any baggage about how they are used or extended by other mods.

- As In Art, So In Life (may need better name). Aesthetics are the A and B of it. Aesthetics are a composite of Beauty, Ideo-Style adherence, Quality, and precepts which we can imagine that emerge from these. Precepts should reflect a polarity about certain things: unused space for the observer, ie. sparsity and grandeur may or may not be relevant. So adhering to a kitsch aesthetic is just as valid for this concept as every pawn sleeping in a private art gallery in a styled modern art bed.
- Luddite. Says what it means. They really don't like Electricity or Vehicles, at all. May or may not be beauty rejectionists. They will have their own line of technologies, or, clothings associated with them to keep with the RimWorld line of ridiculous over generalization mixed with cherry-picked abstraction.
- Lammat-linked Ideoligion, thus having a fertility-cult like aspect to it without being overtly a fertility-cult. To blur what we're talking about from anything in particular they'll have some odd symbols and items, the mirror and comb being among them. We'll introduce these items to the game anyway in one or another mod of ours we're developing. We'll keep gender dynamics about it on an unrealistic with regards Earth, pattern. It marries well with GenderSuperiority, either way and is perfectly fine without it, with a soft-superiority being achievable for the player through ordinary precepts.

Ideology preferences will already allow for preferred garments for either gender, or, no gender preferences. I personally, never gender anything in my mods inherently if I spot it. Players can already do this quickly through Assigns if it is important to them and carefully, through well constructed Precept chains.

The ideal life for a member of this Ideoligion, is to quite literally do nothing except celebrate, lounge around, and let others leave food and valuables for us, or, feed us. A truly successful colony will survive off of nothing but trade, because it acts as a pilgrimage site, essentially, with people drawn there according to the qualities of the shrine the player sets up. They'll always have too many pairs of silk slippers, and, a gift shop which sells silk slippers as well, if Hospitality and my Slippers are active.

This will marry well with an odd precept for how we feel about visitors and visiting, when they are gone, and, different ways to recall or summon WorldPawns via Ideoligion Rituals. A bit of liturgy they might kick around in autogenerated text, would include something like:

"When they come, whomever they be, they'll bear gifts for their betters, and do so in worship."

This marries beautifully with the Guilty precept, and my idea of what do we think while they are gone too :)

If the player wants to play it absolutely PG, they can make this a health spa. SFW.

The follower of an Ideoligion with this Meme, sees their site of commerce and hospitality as fitting of being a shrine to others, and, themselves as stewards or proprietors of whatever is there. I live in a place on a pilgrimage route in Western France, so I get to observe how this affects a village sized cohort living within medieval city walls. We are crazy for inventing festivals and bringing people here, and survive by getting them to return.

This Meme also marries well with say, Trade, Nature, Animal Personhood, perhaps.

- Attendance - what to call this is unclear, but we know what it means. It means personal service, which means an Ideoligion role for attenders: Actors, Performers, Courtsans, the Geisha, essentially, but toned down into a soft and mayhaps literary sounding title. We won't model sex-work, but inevitably someone will as a mod to this meme. So be it - it is part of human history.

To ensure it's kept _cleanish_ according to my standards, which means you could talk about it with someone you met at a bus stop and not expect to be molested, and probably talk about the same thing in Shengzhen at a bus stop with a stranger, this will be an impact category II Meme, which means no way to get there except Fixed Ideoligion with baggage from other Memes which will tend to tone down the overall effect.

If the player wants to open a cathouse in the desert, well, they can technically do that anyway, via other mods. But, textgen will be sensitive that we don't autocreate cathouses in the desert, as this isn't bus stop level safe, which is a good target for All Ages with allowance for careful double-entendre. The Player as always, can shape this narrative how they like.

- Wandering - because others have grabbed "Traveling" and "Nomad." Both of these have peculiar baggage, like having to abandon settlements or craving vehicles. By wandering, I mean, quite literally the way of the _Manouche_ in French.

Got raided here? Damn it. Well, hoof it back to the old place until they leave. Maybe think about how to stop them from raiding us without violence?

The idea is that walking away from a colony to a camp site, is no huge inconvenience. You've already as a group of colonists, survived falling on to a planet, or, walked away from the ruins of your Tribe destroyed by "infernal machines in blood and fire." You know when to move on, how to travel, what to bring, and you don't freak out over minor inconvenciences.

Were Ranching done correctly in the vanillla game, it would work like this to allow for Transhumance as a colony occupation. Factually, people wander with animals and their things from place to place and consider this absolutely normal.

Marries well with a lot of other Memes, from Ideology and Vanilla Expanded Memes.

- Antinomianism, because damn, aren't there already a lot of bandits and such in the game? Where do they come from? Having this Meme in an Ideoligion will come with some direct penalties: - bigger direct penalty of Faction Opinion with the Empire such that without intervention, the Player will inevitably become an enemy like Bandits. A softer penalty will apply to Civil Factions but, over time these too can become enemies. Tribal Factions will have stiffer penalties seeing the Ideoligion's followers as Unruly. To these, you look just like bandits or a hostile tribe who doesn't follow the same rules.

How a Faction possessing this Meme is viewed will likely depend on for how many Precepts they've chosen Honorable or Dishonorable/Abhorrent, demonstrating that at least you have rules of some kind.

Rituals may favor or propose things which are in game totally permissable, but, transgress some Meme or default Precept which is part of your Ideoligion. So, if the dog is your venerated animal, you have a once per year by default, eat a dog, for example, and, must include some analog to Saturnalia as a festival. What form this ritual takes of course depends on the other Memes selected. If all you've selected is Antinomianism, and you choose "don't care" for the many things it makes possible to not care about, then everyone will see you as a Bandit, effectively, after a full game year.

Antinomianism, like Gnosticism, picks up a bunch of baggage which relates to how the faction is perceived by other factions, and nothing else really.

Let's not think about code, let's kick this around using Memes and Precepts as things which we can freely create, if we can fix on a thing or practice which already exists in the game, or, one which we've already added through our own mods.

In particular, let's thread this back to the wearer of the crown and the one who courts them, and focus on this Meme. I think understanding what it contains, implies, and how to achieve it, will give us a guide for some of the other Memes.

So, this is ultimately a mod providing a lot, including Memes if Ideology is active, stuff we can only so far imagine, if Royalty is active, supplement Vanilla Memes Expanded by giving them good Memes to put with aside from Loyalist, which is very boring as implemented in game.

Some of what we invent, will go straight into Mind Matters, if it relates to Needs, Satisfaction, or, how a pawn Experiences, as you've understood by now.