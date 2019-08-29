# RavenBot
This is a Twitch Chat bot for Ravenfall. It is a very slimmed down version of ZerraBot but to only contain the necessary stuff for Ravenfall


## Building 
This was built using .NET Core 2.2 and Visual Studio 2019. You can however use Visual Studio 2017 if you like :-)
Smash CTRL+SHIFT+B to build it!

If you have any issues building, just build  RavenBot.Core first, then RavenBot.Core.Ravenfall and finally RavenBot. 

If you have any other issues regarding the dependencies.
You may have to unselect the project references manually and add them back. I don't know why its like that..


## Running
Make sure you update the settings.json file before running!

```json
{
   // The username of your bot
  "twitchBotUsername": "<twitch bot name>",   
  // the Twitch access token
  // you can generate one here https://twitchtokengenerator.com/
  "twitchBotAuthToken": "<twitch auth stuff here>",
  // the target channel, ex: Zerratar
  "twitchChannel":  "<twitch channel name here>"
  // remove all comments if you copy-paste this, json does not actually support comments.
}
```

then do some crazy stuff like 

```bash
dotnet run --project src\RavenBot\
```

Or just run it as is directly from Visual Studio by hitting F5 :-)

Have fun!