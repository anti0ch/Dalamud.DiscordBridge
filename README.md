# Dalamud.DiscordBridge

A Dalamud plugin that bridges Discord and FFXIV chat, allowing you to send and receive messages between Discord and the game.

## Features

- Send FFXIV chat messages to Discord channels
- Send Discord messages to FFXIV chat channels
- Support for all FFXIV chat channels (/say, /shout, /tell, etc.)
- Support for FFXIV commands (/random, /sit, /cpose, /playdead, /malevolence, etc.)
- Technically if you have plugins that add more /commands, like Teleporter, those should also work (/tp Quarrymill)

## Installation

1. Open Dalamud Settings
2. Go to the "Experimental" tab
3. Add the following custom repository:
   ```
   https://raw.githubusercontent.com/anti0ch/Dalamud.DiscordBridge/master/repo.json
   ```
4. Search for "Discord Bridge" in the plugin installer
5. Click Install

## Configuration

0. (optional) make a new server for just you and your bot to chill in
1. Create a Discord Bot at the [Discord Developer Portal](https://discord.com/developers/applications)
2. Copy your bot token
3. In the plugin settings, paste your bot token
5. Invite the bot to your server with the following permissions (you should be able to generate an invite link when you make that Discord bot. i'll make a more detailed instruction for that later if desired):
   - Send Messages
   - Read Message History
   - Manage Webhooks
   - View Channels
  

## Usage

### Discord to FFXIV

To send a message to FFXIV from Discord, use the following format:
```
xl! /<channel> <message>
```

Examples:
- `xl! /say "Yabbies ain't vilekin!"`
- `xl! /em gesticulates angrily.`
- `xl! /random `

In theory, this should be just like using your chatbox in game, but it doesn't cut you off at the character limit.

Right now, the bot has it where if you do hit the character limit, it'll tell you that it's too long and try to print off what you can say.

I tried to make it split things up automatically and send them piecemeal but there were a few kinks in that so it's not a thing yet.

### FFXIV to Discord

You'll need to set up the bot to post in the channels of whatever server it's in. I just made a new server for me and the bot only.

So let's say you make an #rp channel and you want to have the bot post in there every say, yell, party, shout, and random roll result it has in there.
Inside that #rp channel you would type `xl!setchannel say,yell,p,shout,customemote,standardemote,random` and then stuff would show up in there from now on!

Here's a link to all the chat kinds you can listen to. https://github.com/reiichi001/Dalamud.DiscordBridge/wiki/Chat-kinds

I hope this guide is okay. Let me know if you get stuck on anything.

## Troubleshooting

1. Make sure the bot is online in Discord
2. Verify the bot token is correct
3. Check that the bot has the required permissions
4. Ensure the bot is in the correct channels
5. Check the Dalamud logs for any error messages
