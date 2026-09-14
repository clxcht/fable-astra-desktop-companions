# Fable chan + Astra chan

Private cloud backup of the working **v1.2.0** Windows desktop companion app.

- **Restore the saved setup:** download [the v1.2.0 backup ZIP](Fable-Astra-Backup-v1.2.0-2026-09-14.zip), extract it, and run `Restore.cmd`. It includes the app, all artwork, source, preferences, and startup setting.
- **Get the editable project:** use **Code → Download ZIP** or clone this repository. Source is in `Source/`; run `Build.ps1` to rebuild. The portable app is `FableAstra/FableAstra.exe`.
- **Artwork:** [preview all new action sprites](Action-sprites.png) and [view the conversation cue map](Conversation-art.md).

The v1.1.1 ZIP is kept as an earlier restore point. The included build is Windows ARM64 and needs the .NET 8 Windows Desktop Runtime. API-key values are not included.


Two anime companions with their cropped legs meeting the taskbar edge in the bottom-left corner of Windows. Version 1.2.0 adds 24 action sprites and explicit artwork cues for all 126 offline dialogue lines. The cropped legs and breathing motion remain anchored at the taskbar edge. The desktop view contains only the girls and their speech bubbles; all controls live in their tray icon.

**Already installed on this computer:** find **Fable + Astra** in the Windows Start menu. Startup is enabled for your account. The app runs locally with written offline banter by default.

## Controls

Right-click the **Fable + Astra tray icon** (it may be inside Windows' hidden-icons arrow) for these controls:

- **Pause / Resume:** stop or restart automatic conversations.
- **Next line:** advance one line, including while paused.
- **Have a tea break:** start their tea conversation and drinking poses.
- **Choose a conversation:** replay any of the 21 scenes from its opening line. Choose **Is soup a drink or a meal?** to see the soup experiment.
- **Change poses with conversation:** act out each offline scene with the matching props, keeping them during replies and listening pauses. Disable it to keep the selected resting art throughout.
- **Resting artwork:** original, classic, happy, tea break or talking.
- **Settings:** size, startup, gentle movement, conversation topics, pacing, voices and AI connections.
- **Drag a character:** move the pair. **Reset to bottom left** returns them to the corner.
- **Hide companions:** hide the girls and stop their speech. Double-click their tray icon or launch from Start to restore them.
- **Quit:** exit the app. Startup remains enabled until switched off in Settings.

The collection contains **21 conversations / 126 lines**, including a tea-break opening. Fable is warm and imaginative; Astra is thoughtful and playfully dry. “Surprise me” shuffles every scene before repeating. Offline mode is scripted and makes no network requests. Windows speech is enabled at 35% volume; mute it in the tray menu or choose installed voices in Settings. Automatic voice selection prefers English female voices. On this computer, both use Microsoft Zira with slightly different pacing.

The companions stay above normal desktop windows without stealing keyboard focus. Native window bounds keep them clear of taskbars during dragging, when resizing, and after display changes; auto-hidden taskbars also retain room to open. As with normal Windows desktop overlays, secure sign-in/UAC screens and exclusive fullscreen rendering are controlled by Windows or the fullscreen application.

## Artwork

The default characters come directly from your supplied image. The source is bundled byte-for-byte as `FableAstra/Assets/original.png`. The cutouts have transparent outer backgrounds and enclosed gaps, with white contamination removed from their edges and smooth partial transparency along the contours. Interior colors are preserved. The drawing's existing artist credit is **@thatlev**.

There are now **17 pose pairs / 34 transparent character sprites**. Alongside original, classic, happy, tea and talking, 12 new pairs cover soup, an experiment, baking, snacks, books, notes, space, gardening, games, blankets, cats, and a duck/dragon adventure. These additional drawings were made with the built-in image generator, then their backgrounds and edge contamination were removed with code. The five resting choices remain in the tray; action props appear automatically during authored offline scenes. See `Conversation-art.md` for every dialogue cue and `Artwork-prompts.md` for the exact prompt set. Live AI retains its existing general talking, smiling and tea poses.

## Install, restart, remove

- **Install.cmd** installs to `%LOCALAPPDATA%\FableAstra\App`, creates a Start-menu shortcut, enables sign-in startup and launches the companions. No administrator access is needed.
- For a portable launch, open `FableAstra/FableAstra.exe` and keep its neighboring files and Assets folder together.
- **Uninstall.cmd** stops the installed app, removes its startup entry, Start-menu shortcut and installed program files. It keeps preferences in `%LOCALAPPDATA%\FableAstra\settings.json`.

This build targets **Windows ARM64**, matching this computer, and uses the **.NET 8 Windows Desktop Runtime** already installed here. The included source can be rebuilt for x64 or x86. This is a local build without a commercial code-signing certificate.

## Connect real AI later

Open **Settings → AI connections**. Configure an OpenAI-compatible API base URL and an actual model ID separately for each character, then enable live AI and save.

- Ollama: `http://localhost:11434/v1`, with model IDs you have installed.
- Cloud: your provider's HTTPS API base URL and model ID. Store API keys in Windows user environment variables, such as `FABLE_API_KEY` and `ASTRA_API_KEY`; enter only those variable names in the app.
- Each character receives recent dialogue and its own personality prompt. The art labels are character identities, not evidence that those model names are available from a provider.
- If a request fails, the app reports the connection issue and switches the session to offline banter. Correct the connection in Settings and save to retry. Credentials are never stored in the app's preferences.

The app does not read your screen, files, microphone or browser. Live mode sends the conversation to the endpoints you configure. Voice playback uses Windows speech.

## Source and checks

Editable source is in `Source`. Run `Build.ps1` to rebuild with a .NET SDK that supports `net8.0-windows`. `FableAstra.exe --self-test <output-folder>` checks scene shuffling, alternating speakers, bounded history, endpoint validation, local mock AI replies, HTTP errors, cancellation, pause/resume, 17 art pairs, all 126 action cues, prop continuity, selected-scene playback, timed pose changes, native topmost/no-activation styles, interactive/programmatic taskbar bounds and rendering.

The saved results are in `Verification.json`. Real cloud providers were not called; the live-AI integration was tested using a local mock server. Startup registration and the installed process were checked on this computer; a reboot was not performed.

Implementation references: [Microsoft WPF transparency](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.allowstransparency), [Windows topmost positioning](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos), [drag bounds](https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-moving), [taskbar bounds](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shappbarmessage), [Windows sign-in startup](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys), and [Ollama's OpenAI compatibility](https://docs.ollama.com/api/openai-compatibility).
