# ConfigurableProgressionMessages

Adds some chat messages that you can configure the contents of, along with when they appear in your runs. I made this mod to make it easy for people to make certain points in a run's progression feel a bit more special using a special chat message that gets sent automatically.

By default, a message when you start your first loop is sent.

![demomsg](https://github.com/LordVGames/ConfigurableProgMsgs/assets/51967559/31b9f60f-1dfe-4c68-9eb8-2aacb1fa92f2)


### Config Options
- The message itself
	- Extra messages can be added and one will be randomly picked when the message needs to be sent
- When a certain stage number is reached
	- Whether to send it again after a number of stages
- When a loop starts
	- Either on the first loop or on every loop
- When the Bazaar is visited
	- Either on the first visit or on every visit
- When the Void Fields is visited
	- Either on the first visit or on every visit 
	- AFAIK currently no mod lets you go to void fields multiple times in a run but whenever that happens it should work

All of these are configurable in-game using [Risk of Options](https://thunderstore.io/package/Rune580/Risk_Of_Options/). When you change the contents of a message using it, the message(s) will be shown in the game chat for easy editing.

All messages also support the Unity rich text formatting. You can find all the available formatting options [here](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichTextSupportedTags.html).



I've tested this myself to make sure things work, but there may still be problems. If there are any, or if you got a suggestion for the mod, then create an issue on the [github repo]() or ping me in the RoR2 modding discord.


### Where's the unity project for assetbundle?
I would've put the unity project for the mod's assetbundle in the repo, but it's literally just for the icon for RiskOfOptions (1 file) and I'd rather not increase the repo storage size by 5x (10mb C# solution, 50mb unity project).

I will provide a screenshot of the unity window showing it's just the icon file & assetbundle setup though:

![image](https://github.com/LordVGames/ConfigurableProgMsgs/assets/51967559/f808a883-228c-47f5-8e19-22e8fb5a403e)
