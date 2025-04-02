# Dalamud.DiscordBridge

A Dalamud plugin that bridges Discord and FFXIV chat, allowing you to send and receive messages between Discord and the game.

## Features

- Send FFXIV chat messages to Discord channels
- Send Discord messages to FFXIV chat channels
- Support for all FFXIV chat channels (/say, /shout, /tell, etc.)
- Support for FFXIV commands (/random, /dice, etc.)
- Webhook support for one-way chat forwarding
- Configurable per-channel settings

## Installation

1. Open Dalamud Settings
2. Go to the "Experimental" tab
3. Add the following custom repository:
   ```
   https://raw.githubusercontent.com/yourusername/Dalamud.DiscordBridge/master/repo.json
   ```
4. Search for "Discord Bridge" in the plugin installer
5. Click Install

## Configuration

1. Create a Discord Bot at the [Discord Developer Portal](https://discord.com/developers/applications)
2. Copy your bot token
3. In the plugin settings, paste your bot token
4. Invite the bot to your server with the following permissions:
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

Right now, the bot has it where if you do hit the character limit, it'll just straight up not do anything.

### FFXIV to Discord

Messages from FFXIV will be forwarded to the configured Discord channels based on your settings.

## Troubleshooting

1. Make sure the bot is online in Discord
2. Verify the bot token is correct
3. Check that the bot has the required permissions
4. Ensure the bot is in the correct channels
5. Check the Dalamud logs for any error messages